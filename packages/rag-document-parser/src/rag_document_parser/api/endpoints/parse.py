"""文档解析端点（同步）"""
import time
from typing import Annotated, Any

from fastapi import APIRouter, Depends, File, Form, UploadFile

from rag_document_parser.api.dependencies import RequestId, rate_limit, verify_token
from rag_document_parser.models.responses import ParseResponse, create_error_response, create_success_response
from rag_document_parser.models.schemas import ParseRequest, ParseStrategy
from rag_document_parser.services.config import settings
from rag_document_parser.services.exceptions import FileValidationError, UnsupportedFormatError
from rag_document_parser.services.runtime import get_parser_service
from rag_document_parser.utils.file_utils import (
    SUPPORTED_FORMATS,
    detect_file_type,
    is_file_type_supported,
    save_upload_file,
    validate_file_size,
)
from rag_document_parser.utils.logging import get_logger
from rag_document_parser.utils.validation import validate_parse_request

logger = get_logger(__name__)
router = APIRouter(dependencies=[Depends(rate_limit), Depends(verify_token)])


@router.post("", response_model=ParseResponse)
async def parse_document(
    request_id: RequestId,
    file: Annotated[UploadFile, File(..., description="要解析的文档文件")],
    strategy: Annotated[ParseStrategy, Form(description="解析策略")] = ParseStrategy.AUTO,
    extract_tables: Annotated[bool, Form(description="是否提取表格")] = True,
    extract_images: Annotated[bool, Form(description="是否提取图片信息")] = False,
    ocr_threshold: Annotated[int | None, Form(description="OCR触发阈值")] = None,
    timeout: Annotated[int | None, Form(description="超时时间（秒）")] = None,
) -> dict[str, Any]:
    start_time = time.time()
    temp_file_path = None
    parser_service = get_parser_service()

    try:
        await validate_file_size(file, settings.MAX_UPLOAD_SIZE)
        file_type = await detect_file_type(file)
        if not is_file_type_supported(file_type):
            raise UnsupportedFormatError(file_type.value)

        parse_request = ParseRequest(
            strategy=strategy,
            extract_tables=extract_tables,
            extract_images=extract_images,
            ocr_threshold=ocr_threshold,
            timeout=timeout,
        )
        validate_parse_request(parse_request)

        temp_file_path = await save_upload_file(file, settings.TEMP_DIR)
        parse_result = await parser_service.parse_document(
            file_path=temp_file_path,
            file_type=file_type,
            request=parse_request,
            request_id=request_id,
        )

        processing_time = time.time() - start_time
        return create_success_response(
            data=parse_result,
            processing_time=processing_time,
            metadata={
                "file_type": file_type.value,
                "pages_count": parse_result.document_metadata.page_count,
                "ocr_triggered": parse_result.ocr_triggered,
                "strategy_used": strategy.value,
                "request_id": request_id,
            },
        )
    except (FileValidationError, UnsupportedFormatError) as exc:
        processing_time = time.time() - start_time
        logger.warning(
            "文件验证失败",
            extra={
                "request_id": request_id,
                "file_name": file.filename,
                "error": str(exc),
                "processing_time": processing_time,
            },
        )
        return create_error_response(
            code=exc.code,
            message=exc.message,
            details=exc.details,
            processing_time=processing_time,
            metadata={"request_id": request_id},
        )
    except Exception as exc:
        processing_time = time.time() - start_time
        logger.error(
            "文档解析失败",
            extra={
                "request_id": request_id,
                "file_name": file.filename,
                "error": str(exc),
                "processing_time": processing_time,
            },
            exc_info=exc,
        )
        return create_error_response(
            code="PARSING_FAILED",
            message="文档解析失败",
            details={"error": str(exc)} if settings.DEBUG else None,
            processing_time=processing_time,
            metadata={"request_id": request_id},
        )
    finally:
        if temp_file_path and temp_file_path.exists() and not settings.KEEP_TEMP_FILES:
            try:
                temp_file_path.unlink(missing_ok=True)
            except Exception as exc:
                logger.warning(
                    "清理临时文件失败",
                    extra={
                        "request_id": request_id,
                        "temp_path": str(temp_file_path),
                        "error": str(exc),
                    },
                )


@router.post("/validate")
async def validate_document(
    request_id: RequestId,
    file: Annotated[UploadFile, File(..., description="要验证的文档文件")],
) -> dict[str, Any]:
    start_time = time.time()

    try:
        await validate_file_size(file, settings.MAX_UPLOAD_SIZE)
        file_type = await detect_file_type(file)
        if not is_file_type_supported(file_type):
            raise UnsupportedFormatError(file_type.value)

        return create_success_response(
            data={
                "valid": True,
                "file_type": file_type.value,
                "file_name": file.filename,
                "supported": True,
                "max_size_mb": settings.max_upload_size_mb,
            },
            processing_time=time.time() - start_time,
            metadata={"request_id": request_id},
        )
    except (FileValidationError, UnsupportedFormatError) as exc:
        return create_error_response(
            code=exc.code,
            message=exc.message,
            details=exc.details,
            processing_time=time.time() - start_time,
            metadata={"request_id": request_id},
        )
    except Exception as exc:
        logger.error("文件验证异常", extra={"request_id": request_id, "error": str(exc)}, exc_info=exc)
        return create_error_response(
            code="VALIDATION_FAILED",
            message="文件验证失败",
            details={"error": str(exc)} if settings.DEBUG else None,
            processing_time=time.time() - start_time,
            metadata={"request_id": request_id},
        )


@router.get("/supported-formats")
async def get_supported_formats(request_id: RequestId) -> dict[str, Any]:
    formats_info = {
        file_type: {
            "description": info["description"],
            "extensions": info["extensions"],
            "mime_types": info.get("mime_types", []),
        }
        for file_type, info in SUPPORTED_FORMATS.items()
    }

    return create_success_response(
        data={
            "formats": formats_info,
            "max_upload_size_mb": settings.max_upload_size_mb,
            "temp_dir": str(settings.TEMP_DIR),
        },
        processing_time=0.0,
        metadata={"request_id": request_id},
    )

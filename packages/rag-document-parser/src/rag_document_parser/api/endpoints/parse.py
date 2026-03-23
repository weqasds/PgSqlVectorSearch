"""文档解析端点（同步）"""
import time
from datetime import datetime
from pathlib import Path
from tempfile import NamedTemporaryFile
from typing import Annotated

from fastapi import APIRouter, File, Form, HTTPException, UploadFile
from fastapi.responses import JSONResponse

from rag_document_parser.api.dependencies import (
    FileSizeValidation,
    RequestId,
    get_request_id,
)
from rag_document_parser.models.responses import (
    ParseResponse,
    create_error_response,
    create_success_response,
)
from rag_document_parser.models.schemas import ParseRequest, ParseStrategy
from rag_document_parser.services.config import settings
from rag_document_parser.services.exceptions import (
    FileValidationError,
    UnsupportedFormatError,
)
from rag_document_parser.services.parser_service import ParserService
from rag_document_parser.utils.file_utils import (
    detect_file_type,
    is_file_type_supported,
    save_upload_file,
    validate_file_size,
)
from rag_document_parser.utils.logging import get_logger
from rag_document_parser.utils.validation import validate_parse_request

logger = get_logger(__name__)
router = APIRouter()
parser_service = ParserService()


@router.post("", response_model=ParseResponse)
async def parse_document(
    request_id: RequestId,
    file: Annotated[UploadFile, File(..., description="要解析的文档文件")],
    strategy: Annotated[
        ParseStrategy, Form(description="解析策略")
    ] = ParseStrategy.AUTO,
    extract_tables: Annotated[
        bool, Form(description="是否提取表格")
    ] = True,
    extract_images: Annotated[
        bool, Form(description="是否提取图片信息")
    ] = False,
    ocr_threshold: Annotated[
        int | None, Form(description="OCR触发阈值")
    ] = None,
    timeout: Annotated[
        int | None, Form(description="超时时间（秒）")
    ] = None,
) -> dict:
    """解析文档（同步）"""
    start_time = time.time()
    temp_file_path = None

    try:
        # 验证文件大小
        await validate_file_size(file, settings.MAX_UPLOAD_SIZE)

        # 检测文件类型
        file_type = await detect_file_type(file)
        logger.info(
            "文件上传",
            extra={
                "request_id": request_id,
                "file_name": file.filename,
                "file_type": file_type.value,
                "content_type": file.content_type,
                "strategy": strategy.value,
            },
        )

        # 验证文件类型
        if not is_file_type_supported(file_type):
            raise UnsupportedFormatError(file_type.value)

        # 创建解析请求
        parse_request = ParseRequest(
            strategy=strategy,
            extract_tables=extract_tables,
            extract_images=extract_images,
            ocr_threshold=ocr_threshold or settings.OCR_THRESHOLD,
            timeout=timeout or settings.TASK_TIMEOUT,
        )

        # 验证请求
        validate_parse_request(parse_request)

        # 保存上传文件到临时位置
        temp_file_path = await save_upload_file(file, settings.TEMP_DIR)
        logger.debug(
            "文件保存到临时位置",
            extra={
                "request_id": request_id,
                "temp_path": str(temp_file_path),
                "file_size": temp_file_path.stat().st_size,
            },
        )

        # 解析文档
        logger.info(
            "开始解析文档",
            extra={
                "request_id": request_id,
                "file_type": file_type.value,
                "strategy": strategy.value,
            },
        )

        parse_result = await parser_service.parse_document(
            file_path=temp_file_path,
            file_type=file_type,
            request=parse_request,
            request_id=request_id,
        )

        # 计算处理时间
        processing_time = time.time() - start_time

        logger.info(
            "文档解析完成",
            extra={
                "request_id": request_id,
                "file_name": file.filename,
                "file_type": file_type.value,
                "pages_count": parse_result.document_metadata.page_count,
                "processing_time": processing_time,
                "ocr_triggered": getattr(
                    parse_result, "ocr_triggered", False
                ),
            },
        )

        # 构建响应
        metadata = {
            "file_type": file_type.value,
            "pages_count": parse_result.document_metadata.page_count,
            "ocr_triggered": getattr(parse_result, "ocr_triggered", False),
            "strategy_used": strategy.value,
            "request_id": request_id,
        }

        return create_success_response(
            data=parse_result.dict(),
            processing_time=processing_time,
            metadata=metadata,
        )

    except (FileValidationError, UnsupportedFormatError) as exc:
        # 文件验证错误
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
        # 其他错误
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
        # 清理临时文件
        if temp_file_path and temp_file_path.exists():
            if not settings.KEEP_TEMP_FILES:
                try:
                    temp_file_path.unlink()
                    logger.debug(
                        "清理临时文件",
                        extra={
                            "request_id": request_id,
                            "temp_path": str(temp_file_path),
                        },
                    )
                except Exception as exc:
                    logger.warning(
                        "清理临时文件失败",
                        extra={
                            "request_id": request_id,
                            "temp_path": str(temp_file_path),
                            "error": str(exc),
                        },
                    )
            else:
                logger.debug(
                    "保留临时文件",
                    extra={
                        "request_id": request_id,
                        "temp_path": str(temp_file_path),
                    },
                )


@router.post("/validate")
async def validate_document(
    request_id: RequestId,
    file: Annotated[UploadFile, File(..., description="要验证的文档文件")],
) -> dict:
    """验证文档文件"""
    start_time = time.time()

    try:
        # 验证文件大小
        await validate_file_size(file, settings.MAX_UPLOAD_SIZE)

        # 检测文件类型
        file_type = await detect_file_type(file)

        # 验证文件类型
        if not is_file_type_supported(file_type):
            raise UnsupportedFormatError(file_type.value)

        processing_time = time.time() - start_time

        logger.info(
            "文件验证成功",
            extra={
                "request_id": request_id,
                "file_name": file.filename,
                "file_type": file_type.value,
                "content_type": file.content_type,
                "processing_time": processing_time,
            },
        )

        return create_success_response(
            data={
                "valid": True,
                "file_type": file_type.value,
                "file_name": file.filename,
                "supported": True,
                "max_size_mb": settings.max_upload_size_mb,
            },
            processing_time=processing_time,
            metadata={"request_id": request_id},
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
            "文件验证异常",
            extra={
                "request_id": request_id,
                "file_name": file.filename,
                "error": str(exc),
                "processing_time": processing_time,
            },
            exc_info=exc,
        )

        return create_error_response(
            code="VALIDATION_FAILED",
            message="文件验证失败",
            details={"error": str(exc)} if settings.DEBUG else None,
            processing_time=processing_time,
            metadata={"request_id": request_id},
        )


@router.get("/supported-formats")
async def get_supported_formats(request_id: RequestId) -> dict:
    """获取支持的格式列表"""
    from rag_document_parser.utils.file_utils import SUPPORTED_FORMATS

    formats_info = {}
    for fmt, info in SUPPORTED_FORMATS.items():
        formats_info[fmt] = {
            "description": info["description"],
            "extensions": info["extensions"],
            "mime_types": info.get("mime_types", []),
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
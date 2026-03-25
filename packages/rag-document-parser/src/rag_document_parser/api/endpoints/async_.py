"""异步文档解析端点"""
import time
import uuid
from typing import Annotated, Any

from fastapi import APIRouter, BackgroundTasks, Depends, File, Form, UploadFile

from rag_document_parser.api.dependencies import RequestId, rate_limit, verify_token
from rag_document_parser.models.responses import create_error_response, create_success_response
from rag_document_parser.models.schemas import ParseRequest, ParseStrategy, TaskStatus
from rag_document_parser.services.config import settings
from rag_document_parser.services.exceptions import FileValidationError, UnsupportedFormatError
from rag_document_parser.services.runtime import get_parser_service, get_task_service
from rag_document_parser.utils.file_utils import (
    detect_file_type,
    is_file_type_supported,
    save_upload_file,
    validate_file_size,
)
from rag_document_parser.utils.logging import get_logger
from rag_document_parser.utils.validation import validate_parse_request

logger = get_logger(__name__)
router = APIRouter(dependencies=[Depends(rate_limit), Depends(verify_token)])


@router.post("", response_model=dict[str, Any])
async def parse_document_async(
    background_tasks: BackgroundTasks,
    request_id: RequestId,
    file: Annotated[UploadFile, File(..., description="要解析的文档文件")],
    strategy: Annotated[ParseStrategy, Form(description="解析策略")] = ParseStrategy.AUTO,
    extract_tables: Annotated[bool, Form(description="是否提取表格")] = True,
    extract_images: Annotated[bool, Form(description="是否提取图片信息")] = False,
    ocr_threshold: Annotated[int | None, Form(description="OCR触发阈值")] = None,
    timeout: Annotated[int | None, Form(description="超时时间（秒）")] = None,
) -> dict[str, Any]:
    start_time = time.time()
    task_service = get_task_service()

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
        task_id = str(uuid.uuid4())
        task_service.create_task(
            task_id=task_id,
            request_id=request_id,
            file_path=temp_file_path,
            file_type=file_type,
            parse_request=parse_request,
            temp_file_path=temp_file_path,
            file_name=file.filename,
        )

        background_tasks.add_task(_process_async_task, task_id)

        return create_success_response(
            data={
                "task_id": task_id,
                "status": TaskStatus.PENDING.value,
                "message": "任务已提交，正在处理中",
                "status_url": f"/tasks/{task_id}",
                "result_url": f"/tasks/{task_id}/result",
            },
            processing_time=time.time() - start_time,
            metadata={
                "request_id": request_id,
                "task_id": task_id,
                "file_type": file_type.value,
            },
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
        logger.error(
            "异步任务创建失败",
            extra={"request_id": request_id, "file_name": file.filename, "error": str(exc)},
            exc_info=exc,
        )
        return create_error_response(
            code="ASYNC_TASK_CREATION_FAILED",
            message="异步任务创建失败",
            details={"error": str(exc)} if settings.DEBUG else None,
            processing_time=time.time() - start_time,
            metadata={"request_id": request_id},
        )


async def _process_async_task(task_id: str) -> None:
    task_service = get_task_service()
    parser_service = get_parser_service()
    task_info = task_service.get_task(task_id)
    if not task_info:
        return

    request_id = task_info["request_id"]
    temp_file_path = task_info.get("temp_file_path")
    start_time = time.time()

    try:
        if _is_cancelled(task_id):
            return

        task_service.update_task_status(task_id, TaskStatus.PROCESSING)
        task_service.update_task_progress(task_id, 0.2)

        parse_request = task_info["parse_request"]
        if not isinstance(parse_request, ParseRequest):
            parse_request = ParseRequest.model_validate(parse_request)

        parse_result = await parser_service.parse_document(
            file_path=task_info["file_path"],
            file_type=task_info["file_type"],
            request=parse_request,
            request_id=request_id,
        )

        if _is_cancelled(task_id):
            return

        task_service.update_task_progress(task_id, 0.95)
        task_service.complete_task(
            task_id=task_id,
            result=parse_result,
            processing_time=time.time() - start_time,
        )
    except Exception as exc:
        if _is_cancelled(task_id):
            return
        logger.error(
            "异步任务处理失败",
            extra={"request_id": request_id, "task_id": task_id, "error": str(exc)},
            exc_info=exc,
        )
        task_service.fail_task(
            task_id=task_id,
            error=str(exc),
            processing_time=time.time() - start_time,
        )
    finally:
        if temp_file_path and temp_file_path.exists() and not settings.KEEP_TEMP_FILES:
            try:
                temp_file_path.unlink(missing_ok=True)
            except Exception as exc:
                logger.warning(
                    "清理异步任务临时文件失败",
                    extra={"request_id": request_id, "task_id": task_id, "error": str(exc)},
                )


def _is_cancelled(task_id: str) -> bool:
    task_info = get_task_service().get_task(task_id)
    return bool(task_info and task_info["status"] == TaskStatus.CANCELLED)

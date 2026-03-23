"""异步文档解析端点"""
import asyncio
import time
import uuid
from pathlib import Path
from typing import Annotated, Dict, Any

from fastapi import APIRouter, BackgroundTasks, File, Form, UploadFile
from fastapi.responses import JSONResponse

from rag_document_parser.api.dependencies import (
    FileSizeValidation,
    RequestId,
    get_request_id,
)
from rag_document_parser.models.responses import (
    create_error_response,
    create_success_response,
)
from rag_document_parser.models.schemas import ParseRequest, ParseStrategy, TaskStatus
from rag_document_parser.services.config import settings
from rag_document_parser.services.exceptions import (
    FileValidationError,
    UnsupportedFormatError,
)
from rag_document_parser.services.parser_service import ParserService
from rag_document_parser.services.task_service import TaskService
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
task_service = TaskService()


@router.post("", response_model=Dict[str, Any])
async def parse_document_async(
    background_tasks: BackgroundTasks,
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
    """异步解析文档

    提交文档解析任务，返回任务ID。
    任务将在后台处理，可以通过/tasks/{task_id}查询状态。
    """
    start_time = time.time()
    temp_file_path = None
    task_id = None

    try:
        # 验证文件大小
        await validate_file_size(file, settings.MAX_UPLOAD_SIZE)

        # 检测文件类型
        file_type = await detect_file_type(file)
        logger.info(
            "异步文件上传",
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

        # 创建异步任务
        task_id = str(uuid.uuid4())
        task_service.create_task(
            task_id=task_id,
            request_id=request_id,
            file_path=temp_file_path,
            file_type=file_type,
            parse_request=parse_request.dict(),  # 转换为字典
            temp_file_path=temp_file_path,  # 任务完成后清理
        )

        # 启动后台任务
        background_tasks.add_task(
            _process_async_task,
            task_id=task_id,
            request_id=request_id,
        )

        # 计算处理时间
        processing_time = time.time() - start_time

        logger.info(
            "异步任务已创建",
            extra={
                "request_id": request_id,
                "file_name": file.filename,
                "task_id": task_id,
                "processing_time": processing_time,
            },
        )

        # 返回任务信息
        return create_success_response(
            data={
                "task_id": task_id,
                "status": TaskStatus.PENDING.value,
                "message": "任务已提交，正在处理中",
                "check_status_url": f"/tasks/{task_id}",
                "get_result_url": f"/tasks/{task_id}/result",
            },
            processing_time=processing_time,
            metadata={
                "request_id": request_id,
                "task_id": task_id,
                "file_type": file_type.value,
            },
        )

    except (FileValidationError, UnsupportedFormatError) as exc:
        # 文件验证错误
        processing_time = time.time() - start_time
        logger.warning(
            "异步任务文件验证失败",
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
            "异步任务创建失败",
            extra={
                "request_id": request_id,
                "file_name": file.filename,
                "error": str(exc),
                "processing_time": processing_time,
            },
            exc_info=exc,
        )

        return create_error_response(
            code="ASYNC_TASK_CREATION_FAILED",
            message="异步任务创建失败",
            details={"error": str(exc)} if settings.DEBUG else None,
            processing_time=processing_time,
            metadata={"request_id": request_id},
        )

    finally:
        # 注意：临时文件清理由任务服务在任务完成后处理
        # 这里不需要清理，因为任务服务会处理
        pass


async def _process_async_task(task_id: str, request_id: str):
    """处理异步任务

    这个函数在后台运行，执行实际的文档解析。
    """
    logger.info(
        "开始处理异步任务",
        extra={
            "request_id": request_id,
            "task_id": task_id,
        },
    )

    try:
        # 获取任务信息
        task_info = task_service.get_task(task_id)
        if not task_info:
            logger.error(
                "异步任务不存在",
                extra={
                    "request_id": request_id,
                    "task_id": task_id,
                },
            )
            return

        # 更新任务状态为处理中
        task_service.update_task_status(task_id, TaskStatus.PROCESSING)

        # 开始计时
        start_time = time.time()

        try:
            # 调用解析服务
            parse_result = await parser_service.parse_document(
                file_path=task_info["file_path"],
                file_type=task_info["file_type"],
                request=task_info["parse_request"],
                request_id=request_id,
            )

            # 计算处理时间
            processing_time = time.time() - start_time

            # 保存任务结果
            task_service.complete_task(
                task_id=task_id,
                result=parse_result,
                processing_time=processing_time,
            )

            logger.info(
                "异步任务处理完成",
                extra={
                    "request_id": request_id,
                    "task_id": task_id,
                    "processing_time": processing_time,
                    "pages_count": parse_result.document_metadata.page_count,
                },
            )

        except Exception as parse_exc:
            # 解析失败
            processing_time = time.time() - start_time

            logger.error(
                "异步任务解析失败",
                extra={
                    "request_id": request_id,
                    "task_id": task_id,
                    "error": str(parse_exc),
                    "processing_time": processing_time,
                },
                exc_info=parse_exc,
            )

            task_service.fail_task(
                task_id=task_id,
                error=str(parse_exc),
                processing_time=processing_time,
            )

    except Exception as exc:
        logger.error(
            "异步任务处理异常",
            extra={
                "request_id": request_id,
                "task_id": task_id,
                "error": str(exc),
            },
            exc_info=exc,
        )

        # 更新任务状态为失败
        try:
            task_service.fail_task(
                task_id=task_id,
                error=f"任务处理异常: {str(exc)}",
                processing_time=0,
            )
        except:
            pass

    finally:
        # 清理临时文件
        try:
            task_info = task_service.get_task(task_id)
            if task_info and "temp_file_path" in task_info:
                temp_file_path = task_info["temp_file_path"]
                if temp_file_path and temp_file_path.exists():
                    if not settings.KEEP_TEMP_FILES:
                        temp_file_path.unlink(missing_ok=True)
                        logger.debug(
                            "清理异步任务临时文件",
                            extra={
                                "request_id": request_id,
                                "task_id": task_id,
                                "temp_path": str(temp_file_path),
                            },
                        )
        except Exception as clean_exc:
            logger.warning(
                "清理异步任务临时文件失败",
                extra={
                    "request_id": request_id,
                    "task_id": task_id,
                    "error": str(clean_exc),
                },
            )
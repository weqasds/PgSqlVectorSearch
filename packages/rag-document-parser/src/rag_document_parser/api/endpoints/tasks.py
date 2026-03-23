"""任务管理端点"""
import time
from typing import Annotated, Dict, Any, List, Optional

from fastapi import APIRouter, HTTPException, Path, Query

from rag_document_parser.api.dependencies import RequestId, get_request_id
from rag_document_parser.models.responses import (
    create_error_response,
    create_success_response,
)
from rag_document_parser.models.schemas import TaskStatus
from rag_document_parser.services.config import settings
from rag_document_parser.services.exceptions import DocumentParserError
from rag_document_parser.services.task_service import TaskService
from rag_document_parser.utils.logging import get_logger

logger = get_logger(__name__)
router = APIRouter()
task_service = TaskService()


@router.get("/{task_id}")
async def get_task_status(
    request_id: RequestId,
    task_id: Annotated[str, Path(..., description="任务ID")],
) -> dict:
    """获取任务状态

    查询指定任务的状态和基本信息。
    """
    start_time = time.time()

    try:
        # 获取任务信息
        task_info = task_service.get_task(task_id)
        if not task_info:
            logger.warning(
                "任务不存在",
                extra={
                    "request_id": request_id,
                    "task_id": task_id,
                },
            )

            return create_error_response(
                code="TASK_NOT_FOUND",
                message="任务不存在",
                details={"task_id": task_id},
                processing_time=time.time() - start_time,
                metadata={"request_id": request_id},
            )

        # 构建响应数据
        response_data = {
            "task_id": task_info["task_id"],
            "status": task_info["status"].value,
            "created_at": task_info["created_at"].isoformat(),
            "updated_at": task_info["updated_at"].isoformat(),
            "file_type": task_info["file_type"],
            "processing_time": task_info.get("processing_time"),
        }

        # 添加时间信息
        if task_info.get("started_at"):
            response_data["started_at"] = task_info["started_at"].isoformat()
        if task_info.get("completed_at"):
            response_data["completed_at"] = task_info["completed_at"].isoformat()

        # 添加错误信息（如果有）
        if task_info.get("error"):
            response_data["error"] = task_info["error"]
        if task_info.get("error_details"):
            response_data["error_details"] = task_info["error_details"]

        processing_time = time.time() - start_time

        logger.info(
            "获取任务状态",
            extra={
                "request_id": request_id,
                "task_id": task_id,
                "status": task_info["status"].value,
                "processing_time": processing_time,
            },
        )

        return create_success_response(
            data=response_data,
            processing_time=processing_time,
            metadata={
                "request_id": request_id,
                "task_id": task_id,
            },
        )

    except Exception as exc:
        processing_time = time.time() - start_time
        logger.error(
            "获取任务状态失败",
            extra={
                "request_id": request_id,
                "task_id": task_id,
                "error": str(exc),
                "processing_time": processing_time,
            },
            exc_info=exc,
        )

        return create_error_response(
            code="TASK_STATUS_QUERY_FAILED",
            message="获取任务状态失败",
            details={"error": str(exc)} if settings.DEBUG else None,
            processing_time=processing_time,
            metadata={"request_id": request_id},
        )


@router.get("/{task_id}/result")
async def get_task_result(
    request_id: RequestId,
    task_id: Annotated[str, Path(..., description="任务ID")],
) -> dict:
    """获取任务结果

    获取已完成的文档解析结果。
    """
    start_time = time.time()

    try:
        # 获取任务信息
        task_info = task_service.get_task(task_id)
        if not task_info:
            logger.warning(
                "任务不存在",
                extra={
                    "request_id": request_id,
                    "task_id": task_id,
                },
            )

            return create_error_response(
                code="TASK_NOT_FOUND",
                message="任务不存在",
                details={"task_id": task_id},
                processing_time=time.time() - start_time,
                metadata={"request_id": request_id},
            )

        # 检查任务状态
        if task_info["status"] != TaskStatus.COMPLETED:
            if task_info["status"] == TaskStatus.FAILED:
                error_info = {
                    "error": task_info.get("error", "任务失败"),
                    "error_details": task_info.get("error_details"),
                }

                return create_error_response(
                    code="TASK_FAILED",
                    message="任务执行失败",
                    details=error_info,
                    processing_time=time.time() - start_time,
                    metadata={
                        "request_id": request_id,
                        "task_id": task_id,
                    },
                )
            else:
                return create_error_response(
                    code="TASK_NOT_COMPLETED",
                    message="任务尚未完成",
                    details={
                        "status": task_info["status"].value,
                        "message": "请稍后再试",
                    },
                    processing_time=time.time() - start_time,
                    metadata={
                        "request_id": request_id,
                        "task_id": task_id,
                    },
                )

        # 获取结果
        result = task_info.get("result")
        if not result:
            logger.error(
                "任务结果为空",
                extra={
                    "request_id": request_id,
                    "task_id": task_id,
                },
            )

            return create_error_response(
                code="TASK_RESULT_MISSING",
                message="任务结果不存在",
                details={"task_id": task_id},
                processing_time=time.time() - start_time,
                metadata={"request_id": request_id},
            )

        processing_time = time.time() - start_time

        logger.info(
            "获取任务结果",
            extra={
                "request_id": request_id,
                "task_id": task_id,
                "processing_time": processing_time,
            },
        )

        return create_success_response(
            data=result,
            processing_time=processing_time,
            metadata={
                "request_id": request_id,
                "task_id": task_id,
                "file_type": task_info["file_type"],
                "task_processing_time": task_info.get("processing_time"),
            },
        )

    except Exception as exc:
        processing_time = time.time() - start_time
        logger.error(
            "获取任务结果失败",
            extra={
                "request_id": request_id,
                "task_id": task_id,
                "error": str(exc),
                "processing_time": processing_time,
            },
            exc_info=exc,
        )

        return create_error_response(
            code="TASK_RESULT_QUERY_FAILED",
            message="获取任务结果失败",
            details={"error": str(exc)} if settings.DEBUG else None,
            processing_time=processing_time,
            metadata={"request_id": request_id},
        )


@router.get("")
async def list_tasks(
    request_id: RequestId,
    status: Annotated[
        TaskStatus | None, Query(description="任务状态过滤")
    ] = None,
    limit: Annotated[
        int, Query(description="返回数量限制", ge=1, le=1000)
    ] = 100,
    offset: Annotated[
        int, Query(description="偏移量", ge=0)
    ] = 0,
) -> dict:
    """列出任务

    列出所有任务（支持分页和状态过滤）。
    """
    start_time = time.time()

    try:
        # 获取任务列表
        tasks = task_service.get_all_tasks(status=status, limit=limit, offset=offset)

        # 格式化任务数据
        formatted_tasks = []
        for task in tasks:
            formatted_task = {
                "task_id": task["task_id"],
                "status": task["status"].value,
                "created_at": task["created_at"].isoformat(),
                "updated_at": task["updated_at"].isoformat(),
                "file_type": task["file_type"],
                "processing_time": task.get("processing_time"),
            }

            if task.get("started_at"):
                formatted_task["started_at"] = task["started_at"].isoformat()
            if task.get("completed_at"):
                formatted_task["completed_at"] = task["completed_at"].isoformat()

            formatted_tasks.append(formatted_task)

        # 获取统计信息
        stats = task_service.get_stats()

        processing_time = time.time() - start_time

        logger.info(
            "列出任务",
            extra={
                "request_id": request_id,
                "total_count": len(tasks),
                "status_filter": status.value if status else None,
                "processing_time": processing_time,
            },
        )

        return create_success_response(
            data={
                "tasks": formatted_tasks,
                "pagination": {
                    "limit": limit,
                    "offset": offset,
                    "total": stats["total"],
                },
                "stats": stats,
            },
            processing_time=processing_time,
            metadata={"request_id": request_id},
        )

    except Exception as exc:
        processing_time = time.time() - start_time
        logger.error(
            "列出任务失败",
            extra={
                "request_id": request_id,
                "error": str(exc),
                "processing_time": processing_time,
            },
            exc_info=exc,
        )

        return create_error_response(
            code="TASK_LIST_FAILED",
            message="列出任务失败",
            details={"error": str(exc)} if settings.DEBUG else None,
            processing_time=processing_time,
            metadata={"request_id": request_id},
        )


@router.delete("/{task_id}")
async def delete_task(
    request_id: RequestId,
    task_id: Annotated[str, Path(..., description="任务ID")],
) -> dict:
    """删除任务

    删除指定任务（包括清理临时文件）。
    """
    start_time = time.time()

    try:
        # 检查任务是否存在
        task_info = task_service.get_task(task_id)
        if not task_info:
            logger.warning(
                "删除任务失败：任务不存在",
                extra={
                    "request_id": request_id,
                    "task_id": task_id,
                },
            )

            return create_error_response(
                code="TASK_NOT_FOUND",
                message="任务不存在",
                details={"task_id": task_id},
                processing_time=time.time() - start_time,
                metadata={"request_id": request_id},
            )

        # 清理任务
        success = task_service.cleanup_task(task_id)

        if not success:
            logger.warning(
                "删除任务失败",
                extra={
                    "request_id": request_id,
                    "task_id": task_id,
                },
            )

            return create_error_response(
                code="TASK_DELETE_FAILED",
                message="删除任务失败",
                details={"task_id": task_id},
                processing_time=time.time() - start_time,
                metadata={"request_id": request_id},
            )

        processing_time = time.time() - start_time

        logger.info(
            "任务删除成功",
            extra={
                "request_id": request_id,
                "task_id": task_id,
                "processing_time": processing_time,
            },
        )

        return create_success_response(
            data={
                "task_id": task_id,
                "deleted": True,
                "message": "任务已删除",
            },
            processing_time=processing_time,
            metadata={"request_id": request_id},
        )

    except Exception as exc:
        processing_time = time.time() - start_time
        logger.error(
            "删除任务失败",
            extra={
                "request_id": request_id,
                "task_id": task_id,
                "error": str(exc),
                "processing_time": processing_time,
            },
            exc_info=exc,
        )

        return create_error_response(
            code="TASK_DELETE_FAILED",
            message="删除任务失败",
            details={"error": str(exc)} if settings.DEBUG else None,
            processing_time=processing_time,
            metadata={"request_id": request_id},
        )


@router.get("/stats")
async def get_task_stats(
    request_id: RequestId,
) -> dict:
    """获取任务统计信息

    获取所有任务的统计信息。
    """
    start_time = time.time()

    try:
        stats = task_service.get_stats()

        processing_time = time.time() - start_time

        logger.info(
            "获取任务统计",
            extra={
                "request_id": request_id,
                "processing_time": processing_time,
            },
        )

        return create_success_response(
            data=stats,
            processing_time=processing_time,
            metadata={"request_id": request_id},
        )

    except Exception as exc:
        processing_time = time.time() - start_time
        logger.error(
            "获取任务统计失败",
            extra={
                "request_id": request_id,
                "error": str(exc),
                "processing_time": processing_time,
            },
            exc_info=exc,
        )

        return create_error_response(
            code="TASK_STATS_FAILED",
            message="获取任务统计失败",
            details={"error": str(exc)} if settings.DEBUG else None,
            processing_time=processing_time,
            metadata={"request_id": request_id},
        )
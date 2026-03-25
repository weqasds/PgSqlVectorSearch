"""任务管理端点"""
import time
from typing import Annotated, Any

from fastapi import APIRouter, Depends, Path, Query

from rag_document_parser.api.dependencies import RequestId, rate_limit, verify_token
from rag_document_parser.models.responses import create_error_response, create_success_response
from rag_document_parser.models.schemas import TaskStatus
from rag_document_parser.services.config import settings
from rag_document_parser.services.runtime import get_task_service
from rag_document_parser.utils.logging import get_logger

logger = get_logger(__name__)
router = APIRouter(dependencies=[Depends(rate_limit), Depends(verify_token)])


@router.get("/stats")
async def get_task_stats(request_id: RequestId) -> dict[str, Any]:
    start_time = time.time()
    try:
        stats = get_task_service().get_stats()
        stats["max_tasks"] = settings.MAX_CONCURRENT_TASKS * 10
        return create_success_response(
            data=stats,
            processing_time=time.time() - start_time,
            metadata={"request_id": request_id},
        )
    except Exception as exc:
        logger.error("获取任务统计失败", extra={"request_id": request_id, "error": str(exc)}, exc_info=exc)
        return create_error_response(
            code="TASK_STATS_FAILED",
            message="获取任务统计失败",
            details={"error": str(exc)} if settings.DEBUG else None,
            processing_time=time.time() - start_time,
            metadata={"request_id": request_id},
        )


@router.get("")
async def list_tasks(
    request_id: RequestId,
    status: Annotated[TaskStatus | None, Query(description="任务状态过滤")] = None,
    limit: Annotated[int, Query(description="返回数量限制", ge=1, le=1000)] = 100,
    offset: Annotated[int, Query(description="偏移量", ge=0)] = 0,
) -> dict[str, Any]:
    start_time = time.time()
    try:
        task_service = get_task_service()
        tasks = task_service.get_all_tasks(status=status, limit=limit, offset=offset)
        stats = task_service.get_stats()

        return create_success_response(
            data={
                "tasks": [_serialize_task_summary(task) for task in tasks],
                "pagination": {"limit": limit, "offset": offset, "total": stats["total"]},
                "stats": stats,
            },
            processing_time=time.time() - start_time,
            metadata={"request_id": request_id},
        )
    except Exception as exc:
        logger.error("列出任务失败", extra={"request_id": request_id, "error": str(exc)}, exc_info=exc)
        return create_error_response(
            code="TASK_LIST_FAILED",
            message="列出任务失败",
            details={"error": str(exc)} if settings.DEBUG else None,
            processing_time=time.time() - start_time,
            metadata={"request_id": request_id},
        )


@router.get("/{task_id}")
async def get_task_status(
    request_id: RequestId,
    task_id: Annotated[str, Path(..., description="任务ID")],
) -> dict[str, Any]:
    start_time = time.time()
    task_info = get_task_service().get_task(task_id)
    if not task_info:
        return create_error_response(
            code="TASK_NOT_FOUND",
            message="任务不存在",
            details={"task_id": task_id},
            processing_time=time.time() - start_time,
            metadata={"request_id": request_id},
        )

    return create_success_response(
        data={
            **_serialize_task_summary(task_info),
            "request_id": task_info["request_id"],
            "error": task_info.get("error"),
            "error_details": task_info.get("error_details"),
        },
        processing_time=time.time() - start_time,
        metadata={"request_id": request_id, "task_id": task_id},
    )


@router.get("/{task_id}/result")
async def get_task_result(
    request_id: RequestId,
    task_id: Annotated[str, Path(..., description="任务ID")],
) -> dict[str, Any]:
    start_time = time.time()
    task_info = get_task_service().get_task(task_id)
    if not task_info:
        return create_error_response(
            code="TASK_NOT_FOUND",
            message="任务不存在",
            details={"task_id": task_id},
            processing_time=time.time() - start_time,
            metadata={"request_id": request_id},
        )

    if task_info["status"] == TaskStatus.CANCELLED:
        return create_error_response(
            code="TASK_CANCELLED",
            message="任务已取消",
            details={"task_id": task_id},
            processing_time=time.time() - start_time,
            metadata={"request_id": request_id, "task_id": task_id},
        )
    if task_info["status"] == TaskStatus.FAILED:
        return create_error_response(
            code="TASK_FAILED",
            message="任务执行失败",
            details={"error": task_info.get("error"), "error_details": task_info.get("error_details")},
            processing_time=time.time() - start_time,
            metadata={"request_id": request_id, "task_id": task_id},
        )
    if task_info["status"] != TaskStatus.COMPLETED:
        return create_error_response(
            code="TASK_NOT_COMPLETED",
            message="任务尚未完成",
            details={"status": task_info["status"].value},
            processing_time=time.time() - start_time,
            metadata={"request_id": request_id, "task_id": task_id},
        )

    result = task_info.get("result")
    if not result:
        return create_error_response(
            code="TASK_RESULT_MISSING",
            message="任务结果不存在",
            details={"task_id": task_id},
            processing_time=time.time() - start_time,
            metadata={"request_id": request_id, "task_id": task_id},
        )

    return create_success_response(
        data=result,
        processing_time=time.time() - start_time,
        metadata={
            "request_id": request_id,
            "task_id": task_id,
            "file_type": task_info["file_type"].value,
            "task_processing_time": task_info.get("processing_time"),
        },
    )


@router.post("/{task_id}/cancel")
async def cancel_task(
    request_id: RequestId,
    task_id: Annotated[str, Path(..., description="任务ID")],
) -> dict[str, Any]:
    start_time = time.time()
    task_service = get_task_service()
    task_info = task_service.get_task(task_id)
    if not task_info:
        return create_error_response(
            code="TASK_NOT_FOUND",
            message="任务不存在",
            details={"task_id": task_id},
            processing_time=time.time() - start_time,
            metadata={"request_id": request_id},
        )

    if not task_service.cancel_task(task_id):
        return create_error_response(
            code="TASK_CANCEL_FAILED",
            message="任务当前状态不可取消",
            details={"task_id": task_id, "status": task_info["status"].value},
            processing_time=time.time() - start_time,
            metadata={"request_id": request_id, "task_id": task_id},
        )

    return create_success_response(
        data={"task_id": task_id, "status": TaskStatus.CANCELLED.value},
        processing_time=time.time() - start_time,
        metadata={"request_id": request_id, "task_id": task_id},
    )


@router.delete("/{task_id}")
async def delete_task(
    request_id: RequestId,
    task_id: Annotated[str, Path(..., description="任务ID")],
) -> dict[str, Any]:
    start_time = time.time()
    task_service = get_task_service()
    if not task_service.get_task(task_id):
        return create_error_response(
            code="TASK_NOT_FOUND",
            message="任务不存在",
            details={"task_id": task_id},
            processing_time=time.time() - start_time,
            metadata={"request_id": request_id},
        )

    if not task_service.cleanup_task(task_id):
        return create_error_response(
            code="TASK_DELETE_FAILED",
            message="删除任务失败",
            details={"task_id": task_id},
            processing_time=time.time() - start_time,
            metadata={"request_id": request_id},
        )

    return create_success_response(
        data={"task_id": task_id, "deleted": True},
        processing_time=time.time() - start_time,
        metadata={"request_id": request_id, "task_id": task_id},
    )


def _serialize_task_summary(task: dict[str, Any]) -> dict[str, Any]:
    data = {
        "task_id": task["task_id"],
        "status": task["status"].value,
        "created_at": task["created_at"].isoformat(),
        "updated_at": task["updated_at"].isoformat(),
        "file_type": task["file_type"].value if task.get("file_type") else None,
        "file_name": task.get("file_name"),
        "progress": task.get("progress", 0.0),
        "processing_time": task.get("processing_time"),
    }
    if task.get("started_at"):
        data["started_at"] = task["started_at"].isoformat()
    if task.get("completed_at"):
        data["completed_at"] = task["completed_at"].isoformat()
    return data

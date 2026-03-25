"""任务管理服务"""
from datetime import datetime
from pathlib import Path
from threading import RLock
from typing import Any

from pydantic import BaseModel

from rag_document_parser.models.schemas import FileType, ParseRequest, ParseResult, TaskStatus
from rag_document_parser.services.config import settings
from rag_document_parser.utils.logging import get_logger

logger = get_logger(__name__)


class TaskService:
    """内存版任务管理服务。"""

    def __init__(self) -> None:
        self._tasks: dict[str, dict[str, Any]] = {}
        self._max_tasks = settings.MAX_CONCURRENT_TASKS * 10
        self._lock = RLock()

    def create_task(
        self,
        task_id: str,
        request_id: str,
        file_path: Path,
        file_type: FileType,
        parse_request: ParseRequest,
        temp_file_path: Path | None = None,
        file_name: str | None = None,
    ) -> dict[str, Any]:
        """创建新任务。"""
        now = datetime.now()
        task_info = {
            "task_id": task_id,
            "request_id": request_id,
            "file_path": file_path,
            "file_type": file_type,
            "file_name": file_name,
            "parse_request": parse_request,
            "temp_file_path": temp_file_path,
            "status": TaskStatus.PENDING,
            "created_at": now,
            "updated_at": now,
            "started_at": None,
            "completed_at": None,
            "processing_time": None,
            "progress": 0.0,
            "result": None,
            "error": None,
            "error_details": None,
        }

        with self._lock:
            self._tasks[task_id] = task_info
            self._cleanup_old_tasks()

        logger.info(
            "任务创建成功",
            extra={
                "request_id": request_id,
                "task_id": task_id,
                "file_type": file_type.value,
            },
        )
        return task_info

    def get_task(self, task_id: str) -> dict[str, Any] | None:
        """获取任务信息。"""
        with self._lock:
            return self._tasks.get(task_id)

    def update_task_status(
        self,
        task_id: str,
        status: TaskStatus,
        error: str | None = None,
        error_details: dict[str, Any] | None = None,
    ) -> bool:
        """更新任务状态。"""
        with self._lock:
            task = self._tasks.get(task_id)
            if not task:
                logger.warning("更新任务状态失败：任务不存在", extra={"task_id": task_id})
                return False

            previous_status = task["status"]
            task["status"] = status
            task["updated_at"] = datetime.now()

            if status == TaskStatus.PROCESSING and not task.get("started_at"):
                task["started_at"] = datetime.now()
                task["progress"] = max(task.get("progress", 0.0), 0.1)
            elif status in {TaskStatus.COMPLETED, TaskStatus.FAILED, TaskStatus.CANCELLED}:
                task["progress"] = 1.0
                task["completed_at"] = task.get("completed_at") or datetime.now()

            if error is not None:
                task["error"] = error
                task["error_details"] = error_details

        logger.debug(
            "任务状态更新",
            extra={
                "task_id": task_id,
                "old_status": previous_status.value,
                "new_status": status.value,
            },
        )
        return True

    def update_task_progress(self, task_id: str, progress: float) -> bool:
        """更新任务进度。"""
        with self._lock:
            task = self._tasks.get(task_id)
            if not task:
                return False
            task["progress"] = max(0.0, min(1.0, progress))
            task["updated_at"] = datetime.now()
        return True

    def complete_task(self, task_id: str, result: ParseResult | dict[str, Any], processing_time: float) -> bool:
        """完成任务。"""
        with self._lock:
            task = self._tasks.get(task_id)
            if not task:
                logger.warning("完成任务失败：任务不存在", extra={"task_id": task_id})
                return False

            serialized = result.model_dump(mode="json") if isinstance(result, BaseModel) else result
            task["status"] = TaskStatus.COMPLETED
            task["result"] = serialized
            task["processing_time"] = processing_time
            task["completed_at"] = datetime.now()
            task["updated_at"] = datetime.now()
            task["progress"] = 1.0
            task["error"] = None
            task["error_details"] = None

        logger.info("任务完成", extra={"task_id": task_id, "processing_time": processing_time})
        return True

    def fail_task(
        self,
        task_id: str,
        error: str,
        processing_time: float = 0.0,
        error_details: dict[str, Any] | None = None,
    ) -> bool:
        """标记任务为失败。"""
        with self._lock:
            task = self._tasks.get(task_id)
            if not task:
                logger.warning("标记任务失败失败：任务不存在", extra={"task_id": task_id})
                return False

            task["status"] = TaskStatus.FAILED
            task["error"] = error
            task["error_details"] = error_details
            task["processing_time"] = processing_time
            task["completed_at"] = datetime.now()
            task["updated_at"] = datetime.now()
            task["progress"] = 1.0

        logger.error("任务失败", extra={"task_id": task_id, "error": error})
        return True

    def cancel_task(self, task_id: str) -> bool:
        """取消任务。"""
        with self._lock:
            task = self._tasks.get(task_id)
            if not task:
                logger.warning("取消任务失败：任务不存在", extra={"task_id": task_id})
                return False

            if task["status"] not in {TaskStatus.PENDING, TaskStatus.PROCESSING}:
                logger.warning(
                    "取消任务失败：任务状态不允许取消",
                    extra={"task_id": task_id, "status": task["status"].value},
                )
                return False

            task["status"] = TaskStatus.CANCELLED
            task["updated_at"] = datetime.now()
            task["completed_at"] = datetime.now()
            task["error"] = "任务被取消"
            task["processing_time"] = None
            task["progress"] = 1.0

        logger.info("任务已取消", extra={"task_id": task_id})
        return True

    def get_all_tasks(
        self,
        status: TaskStatus | None = None,
        limit: int = 100,
        offset: int = 0,
    ) -> list[dict[str, Any]]:
        """分页获取任务。"""
        with self._lock:
            tasks = [task for task in self._tasks.values() if not status or task["status"] == status]
        tasks.sort(key=lambda item: item["created_at"], reverse=True)
        return tasks[offset : offset + limit]

    def cleanup_task(self, task_id: str) -> bool:
        """删除任务并清理临时文件。"""
        with self._lock:
            task = self._tasks.get(task_id)
            if not task:
                return False

        temp_path = task.get("temp_file_path")
        if temp_path and temp_path.exists():
            try:
                temp_path.unlink(missing_ok=True)
                logger.debug("清理任务临时文件", extra={"task_id": task_id, "temp_path": str(temp_path)})
            except Exception as exc:
                logger.warning(
                    "清理任务临时文件失败",
                    extra={"task_id": task_id, "temp_path": str(temp_path), "error": str(exc)},
                )

        with self._lock:
            self._tasks.pop(task_id, None)

        logger.debug("任务记录已清理", extra={"task_id": task_id})
        return True

    def _cleanup_old_tasks(self) -> None:
        """清理过期历史任务，防止内存无限增长。"""
        if len(self._tasks) <= self._max_tasks:
            return

        sorted_tasks = sorted(self._tasks.items(), key=lambda item: item[1]["created_at"])
        overflow = len(self._tasks) - self._max_tasks
        deleted_count = 0

        for task_id, task_info in sorted_tasks:
            if task_info["status"] not in {
                TaskStatus.COMPLETED,
                TaskStatus.FAILED,
                TaskStatus.CANCELLED,
            }:
                continue

            temp_path = task_info.get("temp_file_path")
            if temp_path and temp_path.exists():
                try:
                    temp_path.unlink(missing_ok=True)
                except Exception:
                    logger.warning("清理旧任务临时文件失败", extra={"task_id": task_id})

            self._tasks.pop(task_id, None)
            deleted_count += 1
            if deleted_count >= overflow:
                break

        if deleted_count:
            logger.info("清理旧任务", extra={"deleted_count": deleted_count, "remaining_count": len(self._tasks)})

    def get_stats(self) -> dict[str, Any]:
        """获取任务统计信息。"""
        with self._lock:
            tasks = list(self._tasks.values())

        stats = {
            "total": len(tasks),
            "pending": 0,
            "processing": 0,
            "completed": 0,
            "failed": 0,
            "cancelled": 0,
        }
        for task in tasks:
            stats[task["status"].value] += 1
        return stats

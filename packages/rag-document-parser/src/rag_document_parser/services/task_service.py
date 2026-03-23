"""任务管理服务"""
import time
from datetime import datetime
from pathlib import Path
from typing import Dict, Any, Optional, List
import uuid

from rag_document_parser.models.schemas import TaskStatus
from rag_document_parser.services.config import settings
from rag_document_parser.utils.logging import get_logger

logger = get_logger(__name__)


class TaskService:
    """任务管理服务

    管理异步文档解析任务的创建、状态跟踪和结果存储。
    注意：这是一个简单的内存实现，生产环境应该使用Redis或数据库。
    """

    def __init__(self):
        """初始化任务服务"""
        self._tasks: Dict[str, Dict[str, Any]] = {}
        self._max_tasks = settings.MAX_CONCURRENT_TASKS * 10  # 保留一些历史任务

    def create_task(
        self,
        task_id: str,
        request_id: str,
        file_path: Path,
        file_type: str,
        parse_request: Dict[str, Any],
        temp_file_path: Optional[Path] = None,
    ) -> Dict[str, Any]:
        """创建新任务

        Args:
            task_id: 任务ID
            request_id: 请求ID
            file_path: 文件路径
            file_type: 文件类型
            parse_request: 解析请求参数
            temp_file_path: 临时文件路径（可选，用于清理）

        Returns:
            Dict[str, Any]: 任务信息
        """
        task_info = {
            "task_id": task_id,
            "request_id": request_id,
            "file_path": file_path,
            "file_type": file_type,
            "parse_request": parse_request,
            "temp_file_path": temp_file_path,
            "status": TaskStatus.PENDING,
            "created_at": datetime.now(),
            "updated_at": datetime.now(),
            "started_at": None,
            "completed_at": None,
            "processing_time": None,
            "result": None,
            "error": None,
            "error_details": None,
        }

        self._tasks[task_id] = task_info

        # 清理旧任务（防止内存泄漏）
        self._cleanup_old_tasks()

        logger.info(
            "任务创建成功",
            extra={
                "request_id": request_id,
                "task_id": task_id,
                "file_type": file_type,
            },
        )

        return task_info

    def get_task(self, task_id: str) -> Optional[Dict[str, Any]]:
        """获取任务信息

        Args:
            task_id: 任务ID

        Returns:
            Optional[Dict[str, Any]]: 任务信息，如果不存在则返回None
        """
        return self._tasks.get(task_id)

    def update_task_status(
        self,
        task_id: str,
        status: TaskStatus,
        error: Optional[str] = None,
        error_details: Optional[Dict[str, Any]] = None,
    ) -> bool:
        """更新任务状态

        Args:
            task_id: 任务ID
            status: 新状态
            error: 错误信息（如果有）
            error_details: 错误详情（如果有）

        Returns:
            bool: 是否更新成功
        """
        task = self._tasks.get(task_id)
        if not task:
            logger.warning("更新任务状态失败：任务不存在", extra={"task_id": task_id})
            return False

        # 更新状态
        task["status"] = status
        task["updated_at"] = datetime.now()

        # 记录开始时间
        if status == TaskStatus.PROCESSING and not task.get("started_at"):
            task["started_at"] = datetime.now()

        # 记录错误信息
        if error:
            task["error"] = error
            if error_details:
                task["error_details"] = error_details

        logger.debug(
            "任务状态更新",
            extra={
                "task_id": task_id,
                "old_status": task.get("status"),
                "new_status": status.value,
                "request_id": task.get("request_id"),
            },
        )

        return True

    def complete_task(
        self,
        task_id: str,
        result: Dict[str, Any],
        processing_time: float,
    ) -> bool:
        """完成任务

        Args:
            task_id: 任务ID
            result: 解析结果
            processing_time: 处理时间（秒）

        Returns:
            bool: 是否成功
        """
        task = self._tasks.get(task_id)
        if not task:
            logger.warning("完成任务失败：任务不存在", extra={"task_id": task_id})
            return False

        task["status"] = TaskStatus.COMPLETED
        task["result"] = result
        task["processing_time"] = processing_time
        task["completed_at"] = datetime.now()
        task["updated_at"] = datetime.now()
        task["error"] = None
        task["error_details"] = None

        logger.info(
            "任务完成",
            extra={
                "task_id": task_id,
                "request_id": task.get("request_id"),
                "processing_time": processing_time,
            },
        )

        return True

    def fail_task(
        self,
        task_id: str,
        error: str,
        processing_time: float = 0,
        error_details: Optional[Dict[str, Any]] = None,
    ) -> bool:
        """标记任务为失败

        Args:
            task_id: 任务ID
            error: 错误信息
            processing_time: 处理时间（秒）
            error_details: 错误详情

        Returns:
            bool: 是否成功
        """
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

        logger.error(
            "任务失败",
            extra={
                "task_id": task_id,
                "request_id": task.get("request_id"),
                "error": error,
                "processing_time": processing_time,
            },
        )

        return True

    def cancel_task(self, task_id: str) -> bool:
        """取消任务

        Args:
            task_id: 任务ID

        Returns:
            bool: 是否成功
        """
        task = self._tasks.get(task_id)
        if not task:
            logger.warning("取消任务失败：任务不存在", extra={"task_id": task_id})
            return False

        # 只有待处理或处理中的任务可以取消
        if task["status"] not in [TaskStatus.PENDING, TaskStatus.PROCESSING]:
            logger.warning(
                "取消任务失败：任务状态不允许取消",
                extra={
                    "task_id": task_id,
                    "status": task["status"].value,
                },
            )
            return False

        task["status"] = TaskStatus.CANCELLED
        task["updated_at"] = datetime.now()
        task["completed_at"] = datetime.now()
        task["error"] = "任务被取消"
        task["processing_time"] = None

        logger.info(
            "任务已取消",
            extra={
                "task_id": task_id,
                "request_id": task.get("request_id"),
            },
        )

        return True

    def get_all_tasks(
        self,
        status: Optional[TaskStatus] = None,
        limit: int = 100,
        offset: int = 0,
    ) -> List[Dict[str, Any]]:
        """获取所有任务（分页）

        Args:
            status: 过滤状态（可选）
            limit: 返回数量限制
            offset: 偏移量

        Returns:
            List[Dict[str, Any]]: 任务列表
        """
        filtered_tasks = []

        for task in self._tasks.values():
            if status and task["status"] != status:
                continue
            filtered_tasks.append(task)

        # 按创建时间倒序排序
        filtered_tasks.sort(key=lambda x: x["created_at"], reverse=True)

        # 分页
        start = offset
        end = offset + limit
        return filtered_tasks[start:end]

    def cleanup_task(self, task_id: str) -> bool:
        """清理任务（删除任务记录）

        Args:
            task_id: 任务ID

        Returns:
            bool: 是否成功
        """
        if task_id in self._tasks:
            task = self._tasks[task_id]

            # 清理临时文件
            if task.get("temp_file_path"):
                temp_path = task["temp_file_path"]
                if temp_path and temp_path.exists():
                    try:
                        temp_path.unlink(missing_ok=True)
                        logger.debug(
                            "清理任务临时文件",
                            extra={
                                "task_id": task_id,
                                "temp_path": str(temp_path),
                            },
                        )
                    except Exception as exc:
                        logger.warning(
                            "清理任务临时文件失败",
                            extra={
                                "task_id": task_id,
                                "temp_path": str(temp_path),
                                "error": str(exc),
                            },
                        )

            # 删除任务记录
            del self._tasks[task_id]
            logger.debug("任务记录已清理", extra={"task_id": task_id})
            return True

        return False

    def _cleanup_old_tasks(self):
        """清理旧任务（防止内存泄漏）"""
        if len(self._tasks) <= self._max_tasks:
            return

        # 按创建时间排序
        sorted_tasks = sorted(
            self._tasks.items(),
            key=lambda x: x[1]["created_at"]
        )

        # 删除最旧的任务，直到数量在限制内
        tasks_to_delete = len(self._tasks) - self._max_tasks
        deleted_count = 0

        for task_id, task_info in sorted_tasks:
            # 只删除已完成、失败或取消的任务
            if task_info["status"] in [
                TaskStatus.COMPLETED,
                TaskStatus.FAILED,
                TaskStatus.CANCELLED,
            ]:
                # 先清理临时文件
                if task_info.get("temp_file_path"):
                    temp_path = task_info["temp_file_path"]
                    if temp_path and temp_path.exists():
                        try:
                            temp_path.unlink(missing_ok=True)
                        except:
                            pass

                # 删除任务记录
                del self._tasks[task_id]
                deleted_count += 1

                if deleted_count >= tasks_to_delete:
                    break

        if deleted_count > 0:
            logger.info(
                "清理旧任务",
                extra={
                    "deleted_count": deleted_count,
                    "remaining_count": len(self._tasks),
                },
            )

    def get_stats(self) -> Dict[str, Any]:
        """获取任务统计信息

        Returns:
            Dict[str, Any]: 统计信息
        """
        stats = {
            "total": len(self._tasks),
            "pending": 0,
            "processing": 0,
            "completed": 0,
            "failed": 0,
            "cancelled": 0,
        }

        for task in self._tasks.values():
            status = task["status"]
            if status == TaskStatus.PENDING:
                stats["pending"] += 1
            elif status == TaskStatus.PROCESSING:
                stats["processing"] += 1
            elif status == TaskStatus.COMPLETED:
                stats["completed"] += 1
            elif status == TaskStatus.FAILED:
                stats["failed"] += 1
            elif status == TaskStatus.CANCELLED:
                stats["cancelled"] += 1

        return stats
"""API响应模型"""
from datetime import UTC, datetime
from typing import Any, Generic, TypeVar

from pydantic import BaseModel, Field

from .schemas import AsyncParseResponse, HealthCheckResponse, ParseResult, TaskInfo

T = TypeVar("T")


class ErrorDetail(BaseModel):
    """错误详情"""

    code: str = Field(..., description="错误代码")
    message: str = Field(..., description="错误描述")
    details: dict[str, Any] | None = Field(None, description="错误详情")


class Metadata(BaseModel):
    """响应元数据"""

    processing_time: float = Field(..., description="处理耗时（秒）")
    file_type: str | None = Field(None, description="文件类型")
    pages_count: int | None = Field(None, description="页数")
    ocr_triggered: bool | None = Field(None, description="是否触发了OCR")
    strategy_used: str | None = Field(None, description="使用的解析策略")
    request_id: str | None = Field(None, description="请求ID")
    task_id: str | None = Field(None, description="任务ID")
    task_processing_time: float | None = Field(None, description="任务处理耗时")
    timestamp: datetime = Field(
        default_factory=lambda: datetime.now(UTC),
        description="响应时间",
    )


class ApiResponse(BaseModel, Generic[T]):
    """通用API响应"""

    success: bool = Field(..., description="处理是否成功")
    data: T | None = Field(None, description="响应数据")
    error: ErrorDetail | None = Field(None, description="错误信息")
    metadata: Metadata = Field(..., description="响应元数据")


class ParseResponse(ApiResponse[ParseResult]):
    """解析响应"""


class AsyncParseApiResponse(ApiResponse[AsyncParseResponse]):
    """异步解析响应"""


class TaskStatusResponse(ApiResponse[TaskInfo]):
    """任务状态响应"""


class HealthResponse(ApiResponse[HealthCheckResponse]):
    """健康检查响应"""


class SuccessResponse(ApiResponse[dict[str, Any]]):
    """成功响应（通用）"""


class ErrorResponse(ApiResponse[None]):
    """错误响应"""


def _build_metadata(
    processing_time: float,
    metadata: dict[str, Any] | None = None,
) -> Metadata:
    base_metadata = Metadata(processing_time=processing_time)
    if not metadata:
        return base_metadata

    valid_fields = set(Metadata.model_fields)
    updates = {key: value for key, value in metadata.items() if key in valid_fields}
    return base_metadata.model_copy(update=updates)


def create_success_response(
    data: Any,
    processing_time: float = 0.0,
    metadata: dict[str, Any] | None = None,
) -> dict[str, Any]:
    """创建成功响应"""

    meta = _build_metadata(processing_time, metadata)
    payload = data.model_dump(mode="json") if isinstance(data, BaseModel) else data

    return {
        "success": True,
        "data": payload,
        "error": None,
        "metadata": meta.model_dump(mode="json"),
    }


def create_error_response(
    code: str,
    message: str,
    details: dict[str, Any] | None = None,
    processing_time: float = 0.0,
    metadata: dict[str, Any] | None = None,
) -> dict[str, Any]:
    """创建错误响应"""

    meta = _build_metadata(processing_time, metadata)
    return {
        "success": False,
        "data": None,
        "error": {
            "code": code,
            "message": message,
            "details": details,
        },
        "metadata": meta.model_dump(mode="json"),
    }


"""API响应模型"""
from datetime import datetime
from typing import Any, Dict, Generic, Optional, TypeVar

from pydantic import BaseModel, Field
from pydantic.generics import GenericModel

from .schemas import ParseResult, TaskInfo, AsyncParseResponse, HealthCheckResponse

T = TypeVar("T")


class ErrorDetail(BaseModel):
    """错误详情"""
    code: str = Field(..., description="错误代码")
    message: str = Field(..., description="错误描述")
    details: Optional[Dict[str, Any]] = Field(None, description="错误详情")


class Metadata(BaseModel):
    """响应元数据"""
    processing_time: float = Field(..., description="处理耗时（秒）")
    file_type: Optional[str] = Field(None, description="文件类型")
    pages_count: Optional[int] = Field(None, description="页数（仅PDF）")
    ocr_triggered: Optional[bool] = Field(None, description="是否触发了OCR")
    strategy_used: Optional[str] = Field(None, description="使用的解析策略")
    request_id: Optional[str] = Field(None, description="请求ID")
    timestamp: datetime = Field(default_factory=datetime.utcnow, description="响应时间")


class ApiResponse(GenericModel, Generic[T]):
    """通用API响应"""
    success: bool = Field(..., description="处理是否成功")
    data: Optional[T] = Field(None, description="响应数据")
    error: Optional[ErrorDetail] = Field(None, description="错误信息")
    metadata: Metadata = Field(..., description="响应元数据")


# 具体响应类型
class ParseResponse(ApiResponse[ParseResult]):
    """解析响应"""
    pass


class AsyncParseApiResponse(ApiResponse[AsyncParseResponse]):
    """异步解析响应"""
    pass


class TaskStatusResponse(ApiResponse[TaskInfo]):
    """任务状态响应"""
    pass


class HealthResponse(ApiResponse[HealthCheckResponse]):
    """健康检查响应"""
    pass


class SuccessResponse(ApiResponse[Dict[str, Any]]):
    """成功响应（通用）"""
    pass


class ErrorResponse(ApiResponse[None]):
    """错误响应"""
    pass


def create_success_response(
    data: Any,
    processing_time: float = 0.0,
    metadata: Optional[Dict[str, Any]] = None,
) -> Dict[str, Any]:
    """创建成功响应"""
    meta = Metadata(processing_time=processing_time)
    if metadata:
        for key, value in metadata.items():
            if hasattr(meta, key):
                setattr(meta, key, value)

    return {
        "success": True,
        "data": data,
        "error": None,
        "metadata": meta.dict(),
    }


def create_error_response(
    code: str,
    message: str,
    details: Optional[Dict[str, Any]] = None,
    processing_time: float = 0.0,
    metadata: Optional[Dict[str, Any]] = None,
) -> Dict[str, Any]:
    """创建错误响应"""
    meta = Metadata(processing_time=processing_time)
    if metadata:
        for key, value in metadata.items():
            if hasattr(meta, key):
                setattr(meta, key, value)

    return {
        "success": False,
        "data": None,
        "error": {
            "code": code,
            "message": message,
            "details": details,
        },
        "metadata": meta.dict(),
    }
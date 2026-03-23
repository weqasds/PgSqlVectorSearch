"""日志中间件"""
import time
from typing import Callable

from fastapi import Request
from starlette.middleware.base import BaseHTTPMiddleware
from starlette.responses import Response

from rag_document_parser.utils.logging import get_logger

logger = get_logger(__name__)


class LoggingMiddleware(BaseHTTPMiddleware):
    """请求日志中间件"""

    async def dispatch(self, request: Request, call_next: Callable) -> Response:
        # 记录请求开始
        start_time = time.time()
        request_id = (request.state, "request_id", "unknown")

        # 记录请求信息
        logger.info(
            "请求开始",
            extra={
                "request_id": request_id,
                "method": request.method,
                "url": str(request.url),
                "client_ip": request.client.host if request.client else "unknown",
                "user_agent": request.headers.get("user-agent", ""),
            },
        )

        try:
            # 处理请求
            response = await call_next(request)

            # 计算处理时间
            process_time = time.time() - start_time

            # 记录响应信息
            logger.info(
                "请求完成",
                extra={
                    "request_id": request_id,
                    "method": request.method,
                    "url": str(request.url),
                    "status_code": response.status_code,
                    "process_time": process_time,
                    "content_length": response.headers.get("content-length", 0),
                },
            )

            # 添加响应头
            response.headers["X-Process-Time"] = str(process_time)
            response.headers["X-Request-ID"] = request_id

            return response

        except Exception as exc:
            # 计算处理时间
            process_time = time.time() - start_time

            # 记录异常
            logger.error(
                "请求异常",
                extra={
                    "request_id": request_id,
                    "method": request.method,
                    "url": str(request.url),
                    "process_time": process_time,
                    "exception": str(exc),
                },
                exc_info=exc,
            )

            raise


class RequestIDMiddleware(BaseHTTPMiddleware):
    """请求ID中间件"""

    async def dispatch(self, request: Request, call_next: Callable) -> Response:
        import uuid

        # 获取或生成请求ID
        request_id = request.headers.get("X-Request-ID")
        if not request_id:
            request_id = str(uuid.uuid4())

        # 存储到request.state
        request.state.request_id = request_id

        # 继续处理
        response = await call_next(request)

        # 添加响应头
        response.headers["X-Request-ID"] = request_id

        return response

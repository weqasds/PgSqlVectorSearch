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
        start_time = time.time()
        request_id = getattr(request.state, "request_id", "unknown")

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
            response = await call_next(request)
            process_time = time.time() - start_time
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
            response.headers["X-Process-Time"] = f"{process_time:.6f}"
            response.headers["X-Request-ID"] = str(request_id)
            return response
        except Exception as exc:
            process_time = time.time() - start_time
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

        request_id = request.headers.get("X-Request-ID") or str(uuid.uuid4())
        request.state.request_id = request_id
        response = await call_next(request)
        response.headers["X-Request-ID"] = request_id
        return response

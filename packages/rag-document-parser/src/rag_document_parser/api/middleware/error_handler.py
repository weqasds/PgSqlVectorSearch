"""错误处理中间件"""
from typing import Callable

from fastapi import Request, status
from fastapi.responses import JSONResponse
from pydantic import ValidationError
from starlette.middleware.base import BaseHTTPMiddleware
from fastapi.encoders import jsonable_encoder
from rag_document_parser.models.responses import create_error_response
from rag_document_parser.services.exceptions import DocumentParserError, handle_document_parser_error
from rag_document_parser.utils.logging import get_logger

logger = get_logger(__name__)


class ErrorHandlerMiddleware(BaseHTTPMiddleware):
    """错误处理中间件"""

    async def dispatch(self, request: Request, call_next: Callable):
        try:
            return await call_next(request)

        except DocumentParserError as exc:
            # 处理自定义异常
            logger.warning(
                "文档解析错误",
                extra={
                    "request_id": (request.state, "request_id", "unknown"),
                    "error_code": exc.code,
                    "error_message": exc.message,
                    "url": str(request.url),
                },
            )

            error_response = handle_document_parser_error(exc)
            error_response["metadata"]["request_id"] = getattr(
                request.state, "request_id", ""
            )

            return JSONResponse(
                status_code=status.HTTP_400_BAD_REQUEST,
                content=jsonable_encoder(error_response),
            )

        except ValidationError as exc:
            # 处理验证错误
            logger.warning(
                "请求验证错误",
                extra={
                    "request_id": (request.state, "request_id", "unknown"),
                    "errors": exc.errors(),
                    "url": str(request.url),
                },
            )

            error_response = create_error_response(
                code="VALIDATION_ERROR",
                message="请求数据验证失败",
                details={"errors": exc.errors()},
                metadata={"request_id": getattr(request.state, "request_id", "")},
            )

            return JSONResponse(
                status_code=status.HTTP_422_UNPROCESSABLE_ENTITY,
                content=jsonable_encoder(error_response),
            )

        except Exception as exc:
            # 处理其他异常
            logger.error(
                "未处理的异常",
                extra={
                    "request_id": (request.state, "request_id", "unknown"),
                    "url": str(request.url),
                    "method": request.method,
                    "exception_type": type(exc).__name__,
                },
                exc_info=exc,
            )

            error_response = create_error_response(
                code="INTERNAL_SERVER_ERROR",
                message="内部服务器错误",
                details={"exception": str(exc)} if request.app.debug else None,
                metadata={"request_id": getattr(request.state, "request_id", "")},
            )

            return JSONResponse(
                status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
                content=jsonable_encoder(error_response),
            )


class SecurityHeadersMiddleware(BaseHTTPMiddleware):
    """安全头部中间件"""

    async def dispatch(self, request: Request, call_next: Callable):
        response = await call_next(request)

        # 添加安全头部
        response.headers["X-Content-Type-Options"] = "nosniff"
        response.headers["X-Frame-Options"] = "DENY"
        response.headers["X-XSS-Protection"] = "1; mode=block"
        response.headers["Referrer-Policy"] = "strict-origin-when-cross-origin"
        response.headers["Content-Security-Policy"] = "default-src 'self'"

        return response

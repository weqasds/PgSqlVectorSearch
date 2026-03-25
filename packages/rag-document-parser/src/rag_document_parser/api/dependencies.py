"""API依赖注入"""
from collections import defaultdict, deque
import asyncio
import time
from typing import Annotated

from fastapi import Depends, Header, HTTPException, Request, status
from fastapi.security import HTTPAuthorizationCredentials, HTTPBearer

from rag_document_parser.services.config import settings

security = HTTPBearer(auto_error=False)
_rate_limit_records: dict[str, deque[float]] = defaultdict(deque)
_rate_limit_lock = asyncio.Lock()


async def get_request_id(
    request: Request,
    x_request_id: Annotated[str | None, Header(alias="X-Request-ID")] = None,
) -> str:
    """获取请求ID"""
    request_id = x_request_id or str(__import__("uuid").uuid4())[:8]
    request.state.request_id = request_id
    return request_id


async def verify_token(
    credentials: Annotated[HTTPAuthorizationCredentials | None, Depends(security)],
) -> dict | None:
    """验证Bearer Token。"""
    if not settings.ENABLE_AUTH:
        return None

    token = getattr(credentials, "credentials", None) if credentials else None
    if not token:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="未提供认证凭证",
            headers={"WWW-Authenticate": "Bearer"},
        )

    valid_tokens = settings.auth_tokens
    if not valid_tokens:
        raise HTTPException(
            status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
            detail="服务端未配置认证Token",
        )

    if token not in valid_tokens:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="认证凭证无效",
            headers={"WWW-Authenticate": "Bearer"},
        )

    return {"user_id": token[:8], "role": "user"}


async def rate_limit(
    request: Request,
    request_id: Annotated[str, Depends(get_request_id)],
) -> None:
    """请求级速率限制。"""
    if not settings.ENABLE_RATE_LIMIT:
        return

    client_ip = request.client.host if request.client else request_id
    now = time.monotonic()
    window_start = now - 1.0

    async with _rate_limit_lock:
        records = _rate_limit_records[client_ip]
        while records and records[0] < window_start:
            records.popleft()

        if len(records) >= settings.RATE_LIMIT_PER_SECOND:
            raise HTTPException(
                status_code=status.HTTP_429_TOO_MANY_REQUESTS,
                detail="请求过于频繁，请稍后再试",
                headers={"Retry-After": "1"},
            )

        records.append(now)


async def validate_file_size(request: Request) -> None:
    """验证HTTP请求声明的文件大小。"""
    content_length = request.headers.get("content-length")
    if not content_length:
        return

    try:
        file_size = int(content_length)
    except ValueError as exc:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="无效的Content-Length请求头",
        ) from exc

    if file_size > settings.MAX_UPLOAD_SIZE:
        raise HTTPException(
            status_code=status.HTTP_413_REQUEST_ENTITY_TOO_LARGE,
            detail=f"文件大小超过限制 ({settings.max_upload_size_mb:.0f}MB)",
        )


RequestId = Annotated[str, Depends(get_request_id)]
AuthUser = Annotated[dict | None, Depends(verify_token)]
RateLimit = Annotated[None, Depends(rate_limit)]
FileSizeValidation = Annotated[None, Depends(validate_file_size)]

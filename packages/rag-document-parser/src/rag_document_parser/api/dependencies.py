"""API依赖注入"""
from typing import Annotated

from fastapi import Depends, Header, HTTPException, Request
from fastapi.security import HTTPBearer, HTTPAuthorizationCredentials

from rag_document_parser.services.config import settings

# 安全认证（如果需要）
security = HTTPBearer(auto_error=False)


async def get_request_id(
    request: Request,
    x_request_id: Annotated[str | None, Header(alias="X-Request-ID")] = None,
) -> str:
    """获取请求ID"""
    if x_request_id:
        request.state.request_id = x_request_id
        return x_request_id

    # 生成简单的请求ID
    import uuid
    request_id = str(uuid.uuid4())[:8]
    request.state.request_id = request_id
    return request_id


async def verify_token(
    credentials: Annotated[HTTPAuthorizationCredentials | None, Depends(security)],
) -> dict | None:
    """验证Token（如果需要认证）"""
    if not settings.ENABLE_AUTH:
        return None

    if not credentials:
        raise HTTPException(
            status_code=401,
            detail="未提供认证凭证",
            headers={"WWW-Authenticate": "Bearer"},
        )

    # 这里应该实现实际的Token验证逻辑
    # 示例：检查Token是否有效
    token = credentials.credentials

    # TODO: 实现实际的Token验证
    # 暂时返回示例用户信息
    return {"user_id": "demo_user", "role": "user"}


async def rate_limit(
    request: Request,
    request_id: Annotated[str, Depends(get_request_id)],
) -> None:
    """速率限制"""
    if not settings.ENABLE_RATE_LIMIT:
        return

    # TODO: 实现速率限制逻辑
    # 使用Redis或内存存储请求计数
    pass


async def validate_file_size(
    request: Request,
) -> None:
    """验证文件大小"""
    content_length = request.headers.get("content-length")
    if content_length:
        file_size = int(content_length)
        if file_size > settings.MAX_UPLOAD_SIZE:
            raise HTTPException(
                status_code=413,
                detail=f"文件大小超过限制 ({settings.max_upload_size_mb}MB)",
            )


# 依赖类型别名
RequestId = Annotated[str, Depends(get_request_id)]
AuthUser = Annotated[dict | None, Depends(verify_token)]
RateLimit = Annotated[None, Depends(rate_limit)]
FileSizeValidation = Annotated[None, Depends(validate_file_size)]
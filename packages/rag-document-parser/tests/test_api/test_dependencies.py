"""API依赖测试"""
from unittest.mock import Mock

import pytest
from fastapi import HTTPException, Request
from fastapi.security import HTTPAuthorizationCredentials

from rag_document_parser.api.dependencies import (
    _rate_limit_records,
    get_request_id,
    rate_limit,
    validate_file_size,
    verify_token,
)
from rag_document_parser.services.config import settings


@pytest.mark.asyncio
async def test_get_request_id_prefers_header():
    request = Mock(spec=Request)
    request.state = Mock()

    request_id = await get_request_id(request, x_request_id="custom-id")

    assert request_id == "custom-id"
    assert request.state.request_id == "custom-id"


@pytest.mark.asyncio
async def test_verify_token_validates_configured_bearer_token(monkeypatch):
    monkeypatch.setattr(settings, "ENABLE_AUTH", True, raising=False)
    monkeypatch.setattr(settings, "AUTH_TOKENS", "token-a,token-b", raising=False)

    credentials = HTTPAuthorizationCredentials(scheme="Bearer", credentials="token-b")
    user = await verify_token(credentials)

    assert user == {"user_id": "token-b", "role": "user"}


@pytest.mark.asyncio
async def test_verify_token_rejects_invalid_token(monkeypatch):
    monkeypatch.setattr(settings, "ENABLE_AUTH", True, raising=False)
    monkeypatch.setattr(settings, "AUTH_TOKENS", "token-a", raising=False)

    credentials = HTTPAuthorizationCredentials(scheme="Bearer", credentials="bad-token")
    with pytest.raises(HTTPException) as exc_info:
        await verify_token(credentials)

    assert exc_info.value.status_code == 401


@pytest.mark.asyncio
async def test_rate_limit_blocks_after_threshold(monkeypatch):
    _rate_limit_records.clear()
    monkeypatch.setattr(settings, "ENABLE_RATE_LIMIT", True, raising=False)
    monkeypatch.setattr(settings, "RATE_LIMIT_PER_SECOND", 2, raising=False)

    request = Mock(spec=Request)
    request.client = Mock(host="127.0.0.1")

    await rate_limit(request, "req-1")
    await rate_limit(request, "req-2")
    with pytest.raises(HTTPException) as exc_info:
        await rate_limit(request, "req-3")

    assert exc_info.value.status_code == 429


@pytest.mark.asyncio
async def test_validate_file_size_rejects_invalid_content_length():
    request = Mock(spec=Request)
    request.headers = {"content-length": "invalid"}

    with pytest.raises(HTTPException) as exc_info:
        await validate_file_size(request)

    assert exc_info.value.status_code == 400

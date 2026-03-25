"""测试配置"""
import shutil
import uuid
from pathlib import Path

import fitz
import pytest
from fastapi.testclient import TestClient

from rag_document_parser.api.app import app
from rag_document_parser.services.config import settings
from rag_document_parser.services.runtime import get_task_service

RUNTIME_ROOT = Path(__file__).resolve().parent / "_runtime"
RUNTIME_ROOT.mkdir(parents=True, exist_ok=True)


@pytest.fixture
def tmp_path():
    path = RUNTIME_ROOT / uuid.uuid4().hex
    path.mkdir(parents=True, exist_ok=True)
    yield path
    shutil.rmtree(path, ignore_errors=True)


@pytest.fixture(autouse=True)
def reset_runtime_state(tmp_path, monkeypatch):
    temp_dir = tmp_path / "service-temp"
    temp_dir.mkdir(parents=True, exist_ok=True)

    monkeypatch.setattr(settings, "ENABLE_AUTH", False, raising=False)
    monkeypatch.setattr(settings, "ENABLE_RATE_LIMIT", False, raising=False)
    monkeypatch.setattr(settings, "KEEP_TEMP_FILES", False, raising=False)
    monkeypatch.setattr(settings, "TEMP_DIR", temp_dir, raising=False)

    task_service = get_task_service()
    task_service._tasks.clear()
    yield
    task_service._tasks.clear()


@pytest.fixture
def client():
    with TestClient(app) as test_client:
        yield test_client


@pytest.fixture
def sample_pdf_bytes() -> bytes:
    document = fitz.open()
    page = document.new_page()
    page.insert_text((72, 72), "Hello PDF parser")
    data = document.tobytes()
    document.close()
    return data


@pytest.fixture
def sample_pdf_file(tmp_path, sample_pdf_bytes):
    pdf_path = tmp_path / "sample.pdf"
    pdf_path.write_bytes(sample_pdf_bytes)
    return pdf_path


@pytest.fixture
def sample_text_bytes() -> bytes:
    return "第一段\n\n第二段".encode("utf-8")

"""文件工具测试"""
from io import BytesIO

import pytest
from starlette.datastructures import UploadFile

from rag_document_parser.models.schemas import FileType
from rag_document_parser.services.exceptions import FileValidationError
from rag_document_parser.utils.file_utils import detect_file_type, save_upload_file


@pytest.mark.asyncio
async def test_detect_file_type_from_pdf_signature():
    upload = UploadFile(filename="sample.bin", file=BytesIO(b"%PDF-1.4\nhello"), headers={"content-type": "application/octet-stream"})

    file_type = await detect_file_type(upload)

    assert file_type == FileType.PDF


@pytest.mark.asyncio
async def test_detect_file_type_supports_image_mime():
    upload = UploadFile(filename="image.jpg", file=BytesIO(b"\xff\xd8\xfftest"), headers={"content-type": "image/jpeg"})

    file_type = await detect_file_type(upload)

    assert file_type in {FileType.JPEG, FileType.JPG}


@pytest.mark.asyncio
async def test_detect_file_type_rejects_unknown_content():
    upload = UploadFile(filename="unknown.bin", file=BytesIO(b"not-known"), headers={"content-type": "application/octet-stream"})

    with pytest.raises(FileValidationError):
        await detect_file_type(upload)


@pytest.mark.asyncio
async def test_save_upload_file_persists_and_rewinds(tmp_path):
    upload = UploadFile(filename="note.txt", file=BytesIO("hello".encode("utf-8")), headers={"content-type": "text/plain"})

    saved_path = await save_upload_file(upload, tmp_path)

    assert saved_path.exists()
    assert saved_path.read_text(encoding="utf-8") == "hello"
    assert upload.file.tell() == 0

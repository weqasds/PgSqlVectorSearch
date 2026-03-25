"""Office引擎测试"""
from io import BytesIO
from pathlib import Path
from types import SimpleNamespace
from unittest.mock import Mock, patch

import pytest
from PIL import Image

from rag_document_parser.engines.office.office_engine import OfficeEngine
from rag_document_parser.engines.office.word_processor import WordDocumentProcessor
from rag_document_parser.models.schemas import (
    DocumentMetadata,
    DocumentStructure,
    FileType,
    PageContent,
    PageMetadata,
    ParseRequest,
    ParseResult,
)
from rag_document_parser.services.exceptions import DependencyError


def _sample_result(file_type: FileType) -> ParseResult:
    return ParseResult(
        content=[PageContent(page=1, text="office", tables=[], images=[], metadata=PageMetadata(page_number=1))],
        document_metadata=DocumentMetadata(
            title="office",
            author=None,
            subject=None,
            keywords=[],
            created_date=None,
            modified_date=None,
            page_count=1,
            file_size=None,
            file_type=file_type,
        ),
        structure=DocumentStructure(),
        ocr_triggered=False,
    )


def test_office_engine_supports_modern_and_legacy_formats():
    engine = OfficeEngine()

    assert engine.supports("docx") is True
    assert engine.supports("doc") is True
    assert engine.supports("xlsx") is True
    assert engine.supports("ppt") is True


def test_convert_legacy_document_requires_libreoffice(monkeypatch, tmp_path):
    engine = OfficeEngine()
    legacy_file = tmp_path / "legacy.doc"
    legacy_file.write_bytes(b"legacy")

    monkeypatch.setattr("rag_document_parser.engines.office.office_engine.settings.ENABLE_OFFICE_CONVERSION", True)
    from rag_document_parser.engines.office import office_engine as office_engine_module

    with patch.object(type(office_engine_module.settings), "get_libreoffice_path", return_value=None):
        with pytest.raises(DependencyError):
            engine._convert_legacy_document(legacy_file, "doc", "req-office")


def test_parse_sync_preserves_original_legacy_file_type(tmp_path):
    engine = OfficeEngine()
    legacy_file = tmp_path / "legacy.doc"
    converted_file = tmp_path / "legacy.docx"
    cleanup_dir = tmp_path / "converted"
    cleanup_dir.mkdir()
    legacy_file.write_bytes(b"legacy")
    converted_file.write_bytes(b"converted")

    processor = Mock()
    processor._parse_sync.return_value = _sample_result(FileType.DOCX)
    engine._processors = {"docx": processor}
    engine._convert_legacy_document = Mock(return_value=(converted_file, cleanup_dir))

    result = engine._parse_sync(legacy_file, ParseRequest(), "req-office")

    assert result.document_metadata.file_type == FileType.DOC


def test_word_processor_extracts_image_metadata():
    processor = WordDocumentProcessor()
    image_buffer = BytesIO()
    Image.new("RGB", (16, 8), color="white").save(image_buffer, format="PNG")
    target_part = SimpleNamespace(partname="/word/media/image1.png", filename="image1.png", blob=image_buffer.getvalue())
    relation = SimpleNamespace(reltype="http://schemas.openxmlformats.org/officeDocument/2006/relationships/image", target_part=target_part, rId="rId1")
    document = SimpleNamespace(part=SimpleNamespace(rels={"rId1": relation}))

    images = processor._extract_images_from_doc(document, "req-word")

    assert len(images) == 1
    assert images[0].description.startswith("image1.png")



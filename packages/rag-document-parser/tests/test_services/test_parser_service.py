"""解析服务测试"""
from dataclasses import dataclass
from pathlib import Path
from unittest.mock import patch

import pytest

from rag_document_parser.models.schemas import (
    DocumentMetadata,
    DocumentStructure,
    FileType,
    PageContent,
    PageMetadata,
    ParseRequest,
    ParseResult,
    ParseStrategy,
    Table,
)
from rag_document_parser.services.config import settings
from rag_document_parser.services.parser_service import ParserService


@dataclass
class DummyEngine:
    name: str
    supported_extensions: list[str]
    priority: int
    result: ParseResult

    async def parse(self, file_path, request, request_id):
        return self.result.model_copy(deep=True)

    def get_priority(self):
        return self.priority


class DummyOCREngine:
    name = "tesseract"
    supported_extensions = ["png"]

    async def parse_image(self, image, request, request_id, file_type, source_name):
        return ParseResult(
            content=[
                PageContent(
                    page=1,
                    text="OCR extracted text",
                    tables=[],
                    images=[],
                    metadata=PageMetadata(page_number=1),
                )
            ],
            document_metadata=DocumentMetadata(
                title="ocr",
                author=None,
                subject=None,
                keywords=[],
                created_date=None,
                modified_date=None,
                page_count=1,
                file_size=None,
                file_type=FileType.PDF,
            ),
            structure=DocumentStructure(),
            ocr_triggered=True,
        )

    def get_priority(self):
        return 80


def _build_result(file_type: FileType, text: str, tables: list[Table] | None = None) -> ParseResult:
    return ParseResult(
        content=[
            PageContent(
                page=1,
                text=text,
                tables=tables or [],
                images=[],
                metadata=PageMetadata(page_number=1),
            )
        ],
        document_metadata=DocumentMetadata(
            title="doc",
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


def test_normalize_request_applies_strategy_defaults():
    service = ParserService()

    normalized = service._normalize_request(
        ParseRequest(strategy=ParseStrategy.TEXT_ONLY, extract_tables=True, extract_images=True)
    )

    assert normalized.extract_tables is False
    assert normalized.extract_images is False
    assert normalized.ocr_threshold == settings.OCR_THRESHOLD


def test_merge_pdf_tables_uses_supplement_when_primary_missing_tables():
    service = ParserService()
    primary = _build_result(FileType.PDF, "text")
    supplement = _build_result(FileType.PDF, "text", [Table(table_index=0, rows=[["a", "b"]], bbox=None)])

    merged = service._merge_pdf_tables(primary, supplement)

    assert len(merged.content[0].tables) == 1
    assert merged.content[0].tables[0].rows == [["a", "b"]]


@pytest.mark.asyncio
async def test_parse_document_tables_only_strips_text(tmp_path):
    service = ParserService()
    text_engine = DummyEngine(
        name="text",
        supported_extensions=["txt"],
        priority=60,
        result=_build_result(
            FileType.TXT,
            "plain text",
            [Table(table_index=0, rows=[["v1", "v2"]], bbox=None)],
        ),
    )
    service._engines_by_name = {"text": text_engine}
    service._engines_by_extension = {"txt": [text_engine]}

    file_path = tmp_path / "sample.txt"
    file_path.write_text("hello", encoding="utf-8")

    result = await service.parse_document(
        file_path=file_path,
        file_type=FileType.TXT,
        request=ParseRequest(strategy=ParseStrategy.TABLES_ONLY),
        request_id="req-1",
    )

    assert result.content[0].text == ""
    assert result.content[0].tables[0].rows == [["v1", "v2"]]
    assert result.structure.headings == []


@pytest.mark.asyncio
async def test_parse_document_triggers_pdf_ocr_fallback(sample_pdf_file):
    service = ParserService()
    primary_engine = DummyEngine(
        name="pymupdf",
        supported_extensions=["pdf"],
        priority=100,
        result=_build_result(FileType.PDF, ""),
    )
    service._engines_by_name = {"pymupdf": primary_engine, "tesseract": DummyOCREngine()}
    service._engines_by_extension = {"pdf": [primary_engine]}

    with patch.object(type(settings), "get_tesseract_path", return_value=Path("C:/Tesseract-OCR/tesseract.exe")):
        result = await service.parse_document(
            file_path=sample_pdf_file,
            file_type=FileType.PDF,
            request=ParseRequest(strategy=ParseStrategy.AUTO, ocr_threshold=5),
            request_id="req-ocr",
        )

    assert result.ocr_triggered is True
    assert result.content[0].text == "OCR extracted text"


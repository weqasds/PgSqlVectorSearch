"""PDF引擎测试"""
import pytest

from rag_document_parser.engines.pdf.pdfplumber_engine import PDFPlumberEngine
from rag_document_parser.engines.pdf.pymupdf_engine import PyMuPDFEngine
from rag_document_parser.models.schemas import ParseRequest


@pytest.mark.parametrize(
    ("engine_cls", "expected_priority"),
    [(PyMuPDFEngine, 100), (PDFPlumberEngine, 50)],
)
def test_pdf_engines_support_pdf(engine_cls, expected_priority):
    engine = engine_cls()

    assert engine.supports("pdf") is True
    assert engine.get_priority() == expected_priority


@pytest.mark.asyncio
async def test_pymupdf_engine_extracts_text(sample_pdf_file):
    engine = PyMuPDFEngine()
    result = await engine.parse(sample_pdf_file, ParseRequest(extract_tables=False), "req-pdf")

    assert result.document_metadata.file_type.value == "pdf"
    assert "Hello PDF parser" in result.content[0].text


@pytest.mark.asyncio
async def test_pdfplumber_engine_extracts_text(sample_pdf_file):
    engine = PDFPlumberEngine()
    result = await engine.parse(sample_pdf_file, ParseRequest(extract_tables=False), "req-plumber")

    assert result.document_metadata.file_type.value == "pdf"
    assert "Hello PDF parser" in result.content[0].text

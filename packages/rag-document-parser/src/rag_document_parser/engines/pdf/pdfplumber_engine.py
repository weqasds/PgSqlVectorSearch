"""PDFPlumber PDF解析引擎。"""
import asyncio
import re
import traceback
from datetime import datetime, timedelta, timezone
from pathlib import Path

import pdfplumber
from pdfplumber.page import Page

from rag_document_parser.engines.base import BaseParserEngine
from rag_document_parser.models.schemas import (
    BoundingBox,
    DocumentMetadata,
    DocumentStructure,
    FileType,
    Heading,
    ImageInfo,
    PageContent,
    PageMetadata,
    Paragraph,
    ParseRequest,
    ParseResult,
    Table,
)
from rag_document_parser.services.exceptions import ParsingFailedError


class PDFPlumberEngine(BaseParserEngine):
    """专注表格和版面信息的PDF引擎。"""

    def __init__(self) -> None:
        super().__init__("pdfplumber")
        self.supported_extensions = ["pdf"]

    async def parse(self, file_path: Path, request: ParseRequest, request_id: str) -> ParseResult:
        self.log_info(
            "开始PDFPlumber解析",
            extra={"request_id": request_id, "file_path": str(file_path), "extract_tables": request.extract_tables},
        )
        try:
            loop = asyncio.get_running_loop()
            result = await loop.run_in_executor(None, self._parse_sync, file_path, request)
            self.log_info(
                "PDFPlumber解析完成",
                extra={
                    "request_id": request_id,
                    "file_path": str(file_path),
                    "tables_found": sum(len(page.tables) for page in result.content),
                },
            )
            return result
        except Exception as exc:
            self.log_error(
                "PDFPlumber解析失败",
                extra={"request_id": request_id, "file_path": str(file_path), "error": str(exc), "traceback": traceback.format_exc()},
            )
            raise ParsingFailedError(
                f"PDFPlumber解析失败: {str(exc)}",
                details={"engine": self.name, "file_path": str(file_path)},
            ) from exc

    def _parse_sync(self, file_path: Path, request: ParseRequest) -> ParseResult:
        with pdfplumber.open(file_path) as pdf:
            metadata = self._extract_metadata(pdf, file_path)
            pages_content = [
                self._extract_page_content(page, page_num, request)
                for page_num, page in enumerate(pdf.pages)
            ]
            structure = self._extract_structure(pages_content)
            return ParseResult(
                content=pages_content,
                document_metadata=metadata,
                structure=structure,
                ocr_triggered=False,
            )

    def _parse_pdf_date(self, date_str: str | None) -> datetime | None:
        if not date_str:
            return None
        match = re.match(
            r"D:(\d{4})(\d{2})(\d{2})(\d{2})(\d{2})(\d{2})([+-]\d{2})'(\d{2})'",
            date_str,
        )
        if not match:
            return None
        year, month, day, hour, minute, second, tz_hour, tz_minute = match.groups()
        offset = timedelta(hours=int(tz_hour), minutes=int(tz_minute))
        return datetime(
            int(year), int(month), int(day), int(hour), int(minute), int(second), tzinfo=timezone(offset)
        )

    def _extract_metadata(self, pdf: pdfplumber.PDF, file_path: Path) -> DocumentMetadata:
        metadata_dict = pdf.metadata if hasattr(pdf, "metadata") else {}
        return DocumentMetadata(
            title=metadata_dict.get("Title") or file_path.stem,
            author=metadata_dict.get("Author") or None,
            subject=metadata_dict.get("Subject") or None,
            keywords=[item.strip() for item in metadata_dict.get("Keywords", "").split(",") if item.strip()],
            created_date=self._parse_pdf_date(metadata_dict.get("creationDate")),
            modified_date=self._parse_pdf_date(metadata_dict.get("modDate")),
            page_count=len(pdf.pages),
            file_size=file_path.stat().st_size if file_path.exists() else None,
            file_type=FileType.PDF,
        )

    def _extract_page_content(self, page: Page, page_num: int, request: ParseRequest) -> PageContent:
        return PageContent(
            page=page_num + 1,
            text=page.extract_text() or "",
            tables=self._extract_tables(page) if request.extract_tables else [],
            images=self._extract_images(page) if request.extract_images else [],
            metadata=PageMetadata(
                dimensions={"width": float(page.width), "height": float(page.height)},
                rotation=0,
                page_number=page_num + 1,
            ),
        )

    def _extract_tables(self, page: Page) -> list[Table]:
        tables: list[Table] = []
        try:
            found_tables = page.find_tables()
            for index, table in enumerate(found_tables):
                rows = table.extract()
                cleaned_rows = [
                    [str(cell).strip() if cell is not None else "" for cell in row]
                    for row in rows
                ]
                bbox = None
                if getattr(table, "bbox", None):
                    x1, y1, x2, y2 = table.bbox
                    bbox = BoundingBox(x1=float(x1), y1=float(y1), x2=float(x2), y2=float(y2))
                if cleaned_rows:
                    tables.append(Table(table_index=index, rows=cleaned_rows, bbox=bbox))
        except Exception:
            for index, table_data in enumerate(page.extract_tables() or []):
                cleaned_rows = [
                    [str(cell).strip() if cell is not None else "" for cell in row]
                    for row in table_data
                ]
                if cleaned_rows:
                    tables.append(Table(table_index=index, rows=cleaned_rows, bbox=None))
        return tables

    def _extract_images(self, page: Page) -> list[ImageInfo]:
        images: list[ImageInfo] = []
        for index, image in enumerate(page.images):
            bbox = BoundingBox(
                x1=float(image.get("x0", 0.0)),
                y1=float(image.get("top", 0.0)),
                x2=float(image.get("x1", 0.0)),
                y2=float(image.get("bottom", 0.0)),
            )
            images.append(ImageInfo(image_index=index, description=f"图片{index + 1}", bbox=bbox))
        return images

    def _extract_structure(self, pages_content: list[PageContent]) -> DocumentStructure:
        headings: list[Heading] = []
        paragraphs: list[Paragraph] = []
        for page_content in pages_content:
            for block in page_content.text.split("\n\n"):
                block_text = block.strip()
                if not block_text:
                    continue
                lines = block_text.splitlines()
                if len(lines) == 1 and len(block_text) <= 80:
                    headings.append(Heading(level=1 if len(block_text) <= 40 else 2, text=block_text, page=page_content.page, bbox=None))
                else:
                    paragraphs.append(Paragraph(text=block_text, page=page_content.page, bbox=None))
        return DocumentStructure(headings=headings, paragraphs=paragraphs)

    def supports(self, file_extension: str) -> bool:
        return file_extension.lower() in self.supported_extensions

    def get_priority(self) -> int:
        return 50

"""PyMuPDF PDF解析引擎"""
import asyncio
import re
import traceback
from datetime import datetime, timedelta, timezone
from pathlib import Path

import fitz

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


class PyMuPDFEngine(BaseParserEngine):
    """基于PyMuPDF的PDF主解析引擎。"""

    def __init__(self) -> None:
        super().__init__("pymupdf")
        self.supported_extensions = ["pdf"]

    async def parse(self, file_path: Path, request: ParseRequest, request_id: str) -> ParseResult:
        self.log_info(
            "开始解析PDF文档",
            extra={"request_id": request_id, "file_path": str(file_path), "strategy": request.strategy.value},
        )
        try:
            loop = asyncio.get_running_loop()
            result = await loop.run_in_executor(None, self._parse_sync, file_path, request)
            self.log_info(
                "PDF解析完成",
                extra={
                    "request_id": request_id,
                    "file_path": str(file_path),
                    "pages_count": result.document_metadata.page_count,
                },
            )
            return result
        except Exception as exc:
            self.log_error(
                "PDF解析失败",
                extra={"request_id": request_id, "file_path": str(file_path), "error": str(exc), "traceback": traceback.format_exc()},
            )
            raise ParsingFailedError(
                f"PDF解析失败: {str(exc)}",
                details={"engine": self.name, "file_path": str(file_path)},
            ) from exc

    def _parse_sync(self, file_path: Path, request: ParseRequest) -> ParseResult:
        with fitz.open(file_path) as document:
            metadata = self._extract_metadata(document, file_path)
            pages_content = [
                self._extract_page_content(page, page_num, request)
                for page_num, page in enumerate(document)
            ]
            structure = self._extract_structure(document, pages_content)
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
            int(year),
            int(month),
            int(day),
            int(hour),
            int(minute),
            int(second),
            tzinfo=timezone(offset),
        )

    def _extract_metadata(self, document: fitz.Document, file_path: Path) -> DocumentMetadata:
        metadata_dict = document.metadata
        return DocumentMetadata(
            title=metadata_dict.get("title") or file_path.stem,
            author=metadata_dict.get("author") or None,
            subject=metadata_dict.get("subject") or None,
            keywords=[item.strip() for item in metadata_dict.get("keywords", "").split(",") if item.strip()],
            created_date=self._parse_pdf_date(metadata_dict.get("creationDate")),
            modified_date=self._parse_pdf_date(metadata_dict.get("modDate")),
            page_count=len(document),
            file_size=file_path.stat().st_size if file_path.exists() else None,
            file_type=FileType.PDF,
        )

    def _extract_page_content(self, page: fitz.Page, page_num: int, request: ParseRequest) -> PageContent:
        text = page.get_text("text") or ""
        tables = self._extract_tables(page) if request.extract_tables else []
        images = self._extract_images(page) if request.extract_images else []
        return PageContent(
            page=page_num + 1,
            text=text,
            tables=tables,
            images=images,
            metadata=PageMetadata(
                dimensions={"width": float(page.rect.width), "height": float(page.rect.height)},
                rotation=page.rotation,
                page_number=page_num + 1,
            ),
        )

    def _extract_tables(self, page: fitz.Page) -> list[Table]:
        try:
            finder = page.find_tables()
        except Exception:
            return []

        tables: list[Table] = []
        for index, table in enumerate(getattr(finder, "tables", [])):
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
        return tables

    def _extract_images(self, page: fitz.Page) -> list[ImageInfo]:
        images: list[ImageInfo] = []
        for index, image in enumerate(page.get_images(full=True)):
            xref = image[0]
            rects = page.get_image_rects(xref)
            bbox = None
            if rects:
                rect = rects[0]
                bbox = BoundingBox(x1=float(rect.x0), y1=float(rect.y0), x2=float(rect.x1), y2=float(rect.y1))
            images.append(ImageInfo(image_index=index, description=f"图片{index + 1}", bbox=bbox))
        return images

    def _extract_structure(self, document: fitz.Document, pages_content: list[PageContent]) -> DocumentStructure:
        headings: list[Heading] = []
        paragraphs: list[Paragraph] = []

        try:
            toc = document.get_toc(simple=False)
            for item in toc:
                level, title, page_num, *_ = item
                headings.append(Heading(level=level, text=title, page=page_num, bbox=None))
        except Exception:
            pass

        for page_content in pages_content:
            for block in page_content.text.split("\n\n"):
                block_text = block.strip()
                if not block_text:
                    continue
                lines = block_text.splitlines()
                if len(lines) == 1 and len(block_text) <= 80:
                    headings.append(Heading(level=2, text=block_text, page=page_content.page, bbox=None))
                else:
                    paragraphs.append(Paragraph(text=block_text, page=page_content.page, bbox=None))

        return DocumentStructure(headings=headings, paragraphs=paragraphs)

    def supports(self, file_extension: str) -> bool:
        return file_extension.lower() in self.supported_extensions

    def get_priority(self) -> int:
        return 100

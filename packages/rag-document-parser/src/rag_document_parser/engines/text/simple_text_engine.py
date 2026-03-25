"""纯文本文件解析引擎。"""
import asyncio
from pathlib import Path

from rag_document_parser.engines.base import BaseParserEngine
from rag_document_parser.models.schemas import (
    DocumentMetadata,
    DocumentStructure,
    FileType,
    PageContent,
    PageMetadata,
    Paragraph,
    ParseRequest,
    ParseResult,
)
from rag_document_parser.services.exceptions import ParsingFailedError


class SimpleTextEngine(BaseParserEngine):
    """简单纯文本解析引擎。"""

    def __init__(self) -> None:
        super().__init__("text")
        self.supported_extensions = ["txt"]

    async def parse(self, file_path: Path, request: ParseRequest, request_id: str) -> ParseResult:
        loop = asyncio.get_running_loop()
        try:
            return await loop.run_in_executor(None, self._parse_sync, file_path)
        except Exception as exc:
            raise ParsingFailedError(
                f"文本解析失败: {str(exc)}",
                details={"engine": self.name, "file_path": str(file_path)},
            ) from exc

    def _parse_sync(self, file_path: Path) -> ParseResult:
        text = self._read_text(file_path)
        paragraphs = [
            Paragraph(text=block.strip(), page=1, bbox=None)
            for block in text.split("\n\n")
            if block.strip()
        ]
        metadata = DocumentMetadata(
            title=file_path.stem,
            author=None,
            subject=None,
            keywords=[],
            created_date=None,
            modified_date=None,
            page_count=1,
            file_size=file_path.stat().st_size,
            file_type=FileType.TXT,
        )
        page = PageContent(
            page=1,
            text=text,
            tables=[],
            images=[],
            metadata=PageMetadata(page_number=1),
        )
        return ParseResult(
            content=[page],
            document_metadata=metadata,
            structure=DocumentStructure(headings=[], paragraphs=paragraphs),
            ocr_triggered=False,
        )

    def _read_text(self, file_path: Path) -> str:
        for encoding in ("utf-8", "utf-8-sig", "gb18030", "latin-1"):
            try:
                return file_path.read_text(encoding=encoding)
            except UnicodeDecodeError:
                continue
        return file_path.read_text(errors="ignore")

    def supports(self, file_extension: str) -> bool:
        return file_extension.lower() in self.supported_extensions

    def get_priority(self) -> int:
        return 60

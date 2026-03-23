"""PDFPlumber PDF解析引擎（专注于表格提取）"""
import asyncio
import traceback
from pathlib import Path
from typing import List, Dict, Any, Optional
import tempfile

import pdfplumber
from PIL import Image

from rag_document_parser.engines.base import BaseParserEngine
from rag_document_parser.models.schemas import (
    ParseRequest,
    ParseResult,
    DocumentMetadata,
    PageContent,
    Table,
    ImageInfo
)
from rag_document_parser.services.exceptions import (
    DocumentParserError,
    ParsingFailedError
)
from pdfplumber.page import Page


class PDFPlumberEngine(BaseParserEngine):
    """PDFPlumber PDF解析引擎

    专注于表格提取和精确的文本定位，作为PyMuPDF的补充。
    """

    def __init__(self):
        super().__init__("pdfplumber")
        self.supported_extensions = ["pdf"]

    async def parse(
        self,
        file_path: Path,
        request: ParseRequest,
        request_id: str
    ) -> ParseResult:
        """解析PDF文档（专注于表格提取）

        Args:
            file_path: PDF文件路径
            request: 解析请求参数
            request_id: 请求ID

        Returns:
            ParseResult: 解析结果
        """
        self.log_info(
            "开始PDFPlumber解析",
            extra={
                "request_id": request_id,
                "file_path": str(file_path),
                "extract_tables": request.extract_tables
            }
        )

        try:
            # 异步执行PDF解析
            loop = asyncio.get_event_loop()
            parse_result = await loop.run_in_executor(
                None,
                self._parse_sync,
                file_path,
                request,
                request_id
            )

            self.log_info(
                "PDFPlumber解析完成",
                extra={
                    "request_id": request_id,
                    "file_path": str(file_path),
                    "tables_found": sum(len(page.tables) for page in parse_result.content)
                }
            )

            return parse_result

        except Exception as exc:
            self.log_error(
                "PDFPlumber解析失败",
                extra={
                    "request_id": request_id,
                    "file_path": str(file_path),
                    "error": str(exc),
                    "traceback": traceback.format_exc()
                }
            )
            raise ParsingFailedError(
                f"PDFPlumber解析失败: {str(exc)}",
                details={"engine": self.name}
            )

    def _parse_sync(
        self,
        file_path: Path,
        request: ParseRequest,
        request_id: str
    ) -> ParseResult:
        """同步解析PDF文档"""
        pdf = None
        try:
            pdf = pdfplumber.open(file_path)

            # 提取文档元数据
            metadata = self._extract_metadata(pdf)

            # 提取页面内容
            pages_content = []
            for page_num, page in enumerate(pdf.pages):
                page_content = self._extract_page_content(
                    page, page_num, request, request_id
                )
                pages_content.append(page_content)

            # 提取文档结构
            structure = self._extract_structure(pdf, pages_content)

            # 创建解析结果
            result = ParseResult(
                content=pages_content,
                document_metadata=metadata,
                structure=structure,
                ocr_triggered=False
            )

            return result

        finally:
            if pdf:
                pdf.close()

    def _extract_metadata(self, pdf: pdfplumber.PDF) -> DocumentMetadata:
        """提取文档元数据"""
        # pdfplumber的元数据访问方式
        metadata_dict = pdf.metadata if hasattr(pdf, 'metadata') else {}

        return DocumentMetadata(
            title=metadata_dict.get("Title", ""),
            author=metadata_dict.get("Author", ""),
            subject=metadata_dict.get("Subject", ""),
            keywords=metadata_dict.get("Keywords", "").split(",") if metadata_dict.get("Keywords") else [],
            created_date=metadata_dict.get("CreationDate", ""),
            modified_date=metadata_dict.get("ModDate", ""),
            page_count=len(pdf.pages)
        )

    def _extract_page_content(
        self,
        page: Page,
        page_num: int,
        request: ParseRequest,
        request_id: str
    ) -> PageContent:
        """提取页面内容"""
        # 提取文本
        text = page.extract_text() or ""

        # 提取表格（PDFPlumber的强项）
        tables = []
        if request.extract_tables:
            tables = self._extract_tables(page, page_num, request_id)

        # 提取图片信息
        images = []
        if request.extract_images:
            images = self._extract_images(page, page_num, request_id)

        # 页面元数据
        page_metadata = self._extract_page_metadata(page)

        return PageContent(
            page=page_num + 1,
            text=text,
            tables=tables,
            images=images,
            metadata=page_metadata
        )

    def _extract_tables(
        self,
        page: Page,
        page_num: int,
        request_id: str
    ) -> List[Table]:
        """提取表格 - PDFPlumber的核心功能"""
        tables = []
        try:
            # 使用PDFPlumber的表格检测算法
            page_tables = page.extract_tables()

            for table_index, table_data in enumerate(page_tables):
                try:
                    # 获取表格边界框（PDFPlumber不直接提供，需要估算）
                    # 这里简化处理，实际可能需要更精确的定位

                    # 清理表格数据
                    cleaned_rows = []
                    for row in table_data:
                        cleaned_row = [str(cell).strip() if cell is not None else "" for cell in row]
                        cleaned_rows.append(cleaned_row)

                    # 如果表格有数据
                    if cleaned_rows and any(any(cell for cell in row) for row in cleaned_rows):
                        table_info = Table(
                            table_index=table_index,
                            rows=cleaned_rows,
                            bbox=None  # 占位符，实际实现需要计算边界框
                        )
                        tables.append(table_info)

                        self.log_debug(
                            "表格提取成功",
                            extra={
                                "request_id": request_id,
                                "page": page_num + 1,
                                "table_index": table_index,
                                "rows": len(cleaned_rows),
                                "cols": len(cleaned_rows[0]) if cleaned_rows else 0
                            }
                        )

                except Exception as table_exc:
                    self.log_warning(
                        "单张表格提取失败",
                        extra={
                            "request_id": request_id,
                            "page": page_num + 1,
                            "table_index": table_index,
                            "error": str(table_exc)
                        }
                    )

            self.log_info(
                "表格提取完成",
                extra={
                    "request_id": request_id,
                    "page": page_num + 1,
                    "tables_found": len(tables)
                }
            )

        except Exception as exc:
            self.log_warning(
                "表格提取失败",
                extra={
                    "request_id": request_id,
                    "page": page_num + 1,
                    "error": str(exc)
                }
            )

        return tables

    def _extract_images(
        self,
        page: Page,
        page_num: int,
        request_id: str
    ) -> List[ImageInfo]:
        """提取图片信息"""
        images = []
        try:
            # PDFPlumber提取图片
            page_images = page.images

            for img_index, img_info in enumerate(page_images):
                try:
                    # 提取图片信息
                    x0, top, x1, bottom = img_info["x0"], img_info["top"], img_info["x1"], img_info["bottom"]

                    image_info = ImageInfo(
                        image_index=img_index,
                        description=f"图片{img_index + 1}",
                        bbox=[x0, top, x1, bottom]
                    )
                    images.append(image_info)

                except Exception as img_exc:
                    self.log_warning(
                        "单张图片提取失败",
                        extra={
                            "request_id": request_id,
                            "page": page_num + 1,
                            "image_index": img_index,
                            "error": str(img_exc)
                        }
                    )

        except Exception as exc:
            self.log_warning(
                "图片提取失败",
                extra={
                    "request_id": request_id,
                    "page": page_num + 1,
                    "error": str(exc)
                }
            )

        return images

    def _extract_page_metadata(self, page: Page) -> Dict[str, Any]:
        """提取页面元数据"""
        return {
            "dimensions": {
                "width": page.width,
                "height": page.height
            },
            "rotation": 0,  # pdfplumber不直接提供旋转信息
            "crop_box": list(page.cropbox) if hasattr(page, 'cropbox') else [0, 0, page.width, page.height],
            "media_box": list(page.mediabox) if hasattr(page, 'mediabox') else [0, 0, page.width, page.height]
        }

    def _extract_structure(
        self,
        pdf: pdfplumber.PDF,
        pages_content: List[PageContent]
    ) -> Dict[str, Any]:
        """提取文档结构"""
        headings = []
        paragraphs = []

        try:
            # PDFPlumber不直接提供目录信息
            # 可以从文本中提取标题模式

            # 提取段落信息
            for page_content in pages_content:
                if page_content.text.strip():
                    # 按段落分割
                    text_blocks = page_content.text.strip().split('\n\n')
                    for block in text_blocks:
                        if block.strip():
                            # 简单规则：较短的文本块可能是标题
                            lines = block.strip().split('\n')
                            if len(lines) == 1 and len(block) < 100:
                                # 可能是标题
                                headings.append({
                                    "level": 1 if len(block) < 50 else 2,
                                    "text": block.strip(),
                                    "page": page_content.page,
                                    "bbox": [0, 0, 0, 0]
                                })
                            else:
                                paragraphs.append({
                                    "text": block.strip(),
                                    "page": page_content.page,
                                    "bbox": [0, 0, 0, 0]
                                })

        except Exception as exc:
            self.log_warning(
                "文档结构提取失败",
                extra={"error": str(exc)}
            )

        return {
            "headings": headings,
            "paragraphs": paragraphs
        }

    def supports(self, file_extension: str) -> bool:
        """检查是否支持指定的文件扩展名"""
        return file_extension.lower() in self.supported_extensions

    def get_priority(self) -> int:
        """获取引擎优先级"""
        return 50  # 作为PyMuPDF的补充，优先级较低
"""PyMuPDF PDF解析引擎"""
import asyncio
import json
import traceback
from pathlib import Path
from typing import List, Dict, Any, Optional, Tuple

import fitz  # PyMuPDF
from PIL import Image
import numpy as np

from rag_document_parser.engines.base import BaseParserEngine
from rag_document_parser.models.schemas import (
    ParseRequest,
    ParseResult,
    DocumentMetadata,
    PageContent,
    Table,
    ImageInfo,
    ParseStrategy
)
from rag_document_parser.services.exceptions import (
    DocumentParserError,
    ParsingFailedError
)


class PyMuPDFEngine(BaseParserEngine):
    """PyMuPDF PDF解析引擎

    基于PyMuPDF库，提供高性能PDF解析，支持文本提取、
    元数据获取、图片提取等。
    """

    def __init__(self):
        super().__init__("pymupdf")
        self.supported_extensions = ["pdf"]

    async def parse(
        self,
        file_path: Path,
        request: ParseRequest,
        request_id: str
    ) -> ParseResult:
        """解析PDF文档

        Args:
            file_path: PDF文件路径
            request: 解析请求参数
            request_id: 请求ID

        Returns:
            ParseResult: 解析结果
        """
        self.log_info(
            "开始解析PDF文档",
            extra={
                "request_id": request_id,
                "file_path": str(file_path),
                "strategy": request.strategy.value
            }
        )

        try:
            # 异步执行PDF解析（CPU密集型操作使用线程池）
            loop = asyncio.get_event_loop()
            parse_result = await loop.run_in_executor(
                None,
                self._parse_sync,
                file_path,
                request,
                request_id
            )

            self.log_info(
                "PDF解析完成",
                extra={
                    "request_id": request_id,
                    "file_path": str(file_path),
                    "pages_count": parse_result.document_metadata.page_count
                }
            )

            return parse_result

        except Exception as exc:
            self.log_error(
                "PDF解析失败",
                extra={
                    "request_id": request_id,
                    "file_path": str(file_path),
                    "error": str(exc),
                    "traceback": traceback.format_exc()
                }
            )
            raise ParsingFailedError(
                f"PDF解析失败: {str(exc)}",
                details={"engine": self.name}
            )

    def _parse_sync(
        self,
        file_path: Path,
        request: ParseRequest,
        request_id: str
    ) -> ParseResult:
        """同步解析PDF文档（在executor中运行）"""
        doc = None
        try:
            # 打开PDF文档
            doc = fitz.open(file_path)

            # 提取文档元数据
            metadata = self._extract_metadata(doc)

            # 提取页面内容
            pages_content = []
            for page_num in range(len(doc)):
                page = doc[page_num]
                page_content = self._extract_page_content(
                    page, page_num, request, request_id
                )
                pages_content.append(page_content)

            # 提取文档结构（标题、段落等）
            structure = self._extract_structure(doc, pages_content)

            # 创建解析结果
            result = ParseResult(
                content=pages_content,
                document_metadata=metadata,
                structure=structure,
                ocr_triggered=False  # PyMuPDF使用原生文本提取，不触发OCR
            )

            return result

        finally:
            if doc:
                doc.close()

    def _extract_metadata(self, doc: fitz.Document) -> DocumentMetadata:
        """提取文档元数据"""
        metadata_dict = doc.metadata

        return DocumentMetadata(
            title=metadata_dict.get("title", ""),
            author=metadata_dict.get("author", ""),
            subject=metadata_dict.get("subject", ""),
            keywords=metadata_dict.get("keywords", "").split(",") if metadata_dict.get("keywords") else [],
            created_date=metadata_dict.get("creationDate", ""),
            modified_date=metadata_dict.get("modDate", ""),
            page_count=len(doc)
        )

    def _extract_page_content(
        self,
        page: fitz.Page,
        page_num: int,
        request: ParseRequest,
        request_id: str
    ) -> PageContent:
        """提取页面内容"""
        # 提取文本
        text = page.get_text()

        # 初始化表格和图片列表
        tables = []
        images = []

        # 提取表格（如果启用）
        if request.extract_tables:
            tables = self._extract_tables(page, page_num, request_id)

        # 提取图片信息（如果启用）
        if request.extract_images:
            images = self._extract_images(page, page_num, request_id)

        # 获取页面元数据
        page_metadata = self._extract_page_metadata(page)

        return PageContent(
            page=page_num + 1,  # 页面编号从1开始
            text=text,
            tables=tables,
            images=images,
            metadata=page_metadata
        )

    def _extract_tables(
        self,
        page: fitz.Page,
        page_num: int,
        request_id: str
    ) -> List[Table]:
        """提取表格

        注意：PyMuPDF的表格检测能力有限，这里使用简单规则检测表格。
        实际生产环境可以集成pdfplumber进行表格提取。
        """
        tables = []
        try:
            # 使用PyMuPDF的get_text提取文本块信息
            blocks = page.get_text("dict")["blocks"]

            # 简单规则：检测可能包含表格的区域
            # 实际实现可能需要更复杂的算法
            for block in blocks:
                if block["type"] == 0:  # 文本块
                    # 检查是否有类似表格的结构（多行对齐的文本）
                    # 这里只是一个简单示例
                    pass

            # 如果启用了高级表格检测，可以调用pdfplumber
            # 这里暂时返回空列表

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
        page: fitz.Page,
        page_num: int,
        request_id: str
    ) -> List[ImageInfo]:
        """提取图片信息"""
        images = []
        try:
            # 获取页面中的所有图片
            image_list = page.get_images()

            for img_index, img_info in enumerate(image_list):
                try:
                    # 提取图片基本信息和边界框
                    # 注意：PyMuPDF中图片定位信息有限
                    xref = img_info[0]

                    # 这里我们可以获取图片的边界框
                    # 实际实现可能需要更复杂的图像处理

                    image_info = ImageInfo(
                        image_index=img_index,
                        description=f"图片{img_index + 1}",
                        bbox=[0, 0, 0, 0]  # 占位符，实际需要计算
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

    def _extract_page_metadata(self, page: fitz.Page) -> Dict[str, Any]:
        """提取页面元数据"""
        rect = page.rect

        return {
            "dimensions": {
                "width": rect.width,
                "height": rect.height
            },
            "rotation": page.rotation,
            "crop_box": list(page.cropbox),
            "media_box": list(page.mediabox)
        }

    def _extract_structure(
        self,
        doc: fitz.Document,
        pages_content: List[PageContent]
    ) -> Dict[str, Any]:
        """提取文档结构（标题、段落等）"""
        headings = []
        paragraphs = []

        try:
            # 提取目录（如果有）
            toc = doc.get_toc()
            for item in toc:
                level, title, page_num, _ = item
                # 查找标题在页面中的位置（简化版）
                headings.append({
                    "level": level,
                    "text": title,
                    "page": page_num,
                    "bbox": [0, 0, 0, 0]  # 占位符
                })

            # 提取段落信息
            for page_content in pages_content:
                if page_content.text.strip():
                    # 简单按换行符分割段落
                    lines = page_content.text.strip().split('\n')
                    for line in lines:
                        if line.strip():
                            paragraphs.append({
                                "text": line.strip(),
                                "page": page_content.page,
                                "bbox": [0, 0, 0, 0]  # 占位符
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
        return 100  # PyMuPDF作为主PDF引擎，优先级最高
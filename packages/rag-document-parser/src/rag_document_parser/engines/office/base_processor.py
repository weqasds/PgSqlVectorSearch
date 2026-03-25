"""Office处理器基类"""
from abc import ABC, abstractmethod
from pathlib import Path
from typing import Optional, Dict, Any, List

from rag_document_parser.models.schemas import (
    ParseRequest,
    ParseResult,
    DocumentMetadata,
    PageContent,
    DocumentStructure,
)
from rag_document_parser.utils.logging import get_logger


class BaseOfficeProcessor(ABC):
    """Office处理器基类"""

    def __init__(self, format_name: str):
        """初始化处理器

        Args:
            format_name: 格式名称（如word, excel, powerpoint）
        """
        self.format_name = format_name
        self.logger = get_logger(f"office.{format_name}")

    @abstractmethod
    async def parse(
        self,
        file_path: Path,
        request: ParseRequest,
        request_id: str
    ) -> ParseResult:
        """解析文档

        Args:
            file_path: 文件路径
            request: 解析请求参数
            request_id: 请求ID

        Returns:
            ParseResult: 解析结果
        """
        pass

    @abstractmethod
    def _extract_metadata(self, document, file_path: Path) -> DocumentMetadata:
        """提取文档元数据

        Args:
            document: 打开的文档对象
            file_path: 文件路径

        Returns:
            DocumentMetadata: 文档元数据
        """
        pass

    @abstractmethod
    def _extract_content(
        self,
        document,
        request: ParseRequest,
        request_id: str
    ) -> List[PageContent]:
        """提取文档内容

        Args:
            document: 打开的文档对象
            request: 解析请求参数
            request_id: 请求ID

        Returns:
            List[PageContent]: 页面内容列表
        """
        pass

    @abstractmethod
    def _extract_structure(
        self,
        document,
        pages_content: List[PageContent],
        request_id: str
    ) -> DocumentStructure:
        """提取文档结构

        Args:
            document: 打开的文档对象
            pages_content: 页面内容列表
            request_id: 请求ID

        Returns:
            DocumentStructure: 文档结构
        """
        pass

    def log_info(self, message: str, extra: Optional[Dict[str, Any]] = None):
        """记录信息日志"""
        self.logger.info(message, extra=extra or {})

    def log_error(self, message: str, extra: Optional[Dict[str, Any]] = None):
        """记录错误日志"""
        self.logger.error(message, extra=extra or {})

    def log_warning(self, message: str, extra: Optional[Dict[str, Any]] = None):
        """记录警告日志"""
        self.logger.warning(message, extra=extra or {})
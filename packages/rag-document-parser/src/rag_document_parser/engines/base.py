"""解析引擎基类"""
from abc import ABC, abstractmethod
from pathlib import Path
from typing import Optional, Dict, Any

from rag_document_parser.models.schemas import ParseRequest, ParseResult
from rag_document_parser.utils.logging import get_logger

logger = get_logger(__name__)


class BaseParserEngine(ABC):
    """解析引擎基类"""

    def __init__(self, name: str):
        """初始化引擎

        Args:
            name: 引擎名称
        """
        self.name = name
        self.logger = get_logger(f"engine.{name}")

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
    def supports(self, file_extension: str) -> bool:
        """检查是否支持指定的文件扩展名

        Args:
            file_extension: 文件扩展名（不含点）

        Returns:
            bool: 是否支持
        """
        pass

    @abstractmethod
    def get_priority(self) -> int:
        """获取引擎优先级（数值越大优先级越高）

        Returns:
            int: 优先级
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

    def log_debug(self, message: str, extra: Optional[Dict[str, Any]] = None):
        """记录调试日志"""
        self.logger.debug(message, extra=extra or {})
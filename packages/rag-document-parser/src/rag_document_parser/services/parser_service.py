"""解析服务 - 协调文档解析流程"""
from pathlib import Path
from typing import Dict, Any, Optional, List, Type
import importlib

from rag_document_parser.models.schemas import (
    ParseRequest,
    FileType,
    ParseResult,
    DocumentMetadata,
    PageContent,
    Table,
    ImageInfo
)
from rag_document_parser.services.exceptions import (
    DocumentParserError,
    UnsupportedFormatError,
    ParsingFailedError
)
from rag_document_parser.utils.logging import get_logger

logger = get_logger(__name__)


class ParserService:
    """文档解析服务（协调各个解析引擎）"""

    def __init__(self):
        """初始化解析服务"""
        self._engines = {}
        self._engine_instances = {}
        self._setup_engines()

    def _setup_engines(self):
        """设置解析引擎"""
        # 注册PDF引擎
        try:
            from rag_document_parser.engines.pdf import PyMuPDFEngine, PDFPlumberEngine
            self._register_engine(PyMuPDFEngine())
            self._register_engine(PDFPlumberEngine())
            logger.info("PDF引擎注册成功")
        except ImportError as e:
            logger.warning(f"PDF引擎导入失败: {e}")

        # TODO: 注册其他引擎（OCR、Office等）
        logger.info(f"引擎初始化完成，已注册 {len(self._engine_instances)} 个引擎")

    def _register_engine(self, engine):
        """注册引擎"""
        engine_name = engine.name
        self._engine_instances[engine_name] = engine

        # 注册引擎支持的扩展名
        for ext in self._get_supported_extensions(engine):
            if ext not in self._engines:
                self._engines[ext] = []
            self._engines[ext].append(engine)

        logger.debug(f"注册引擎: {engine_name}")

    def _get_supported_extensions(self, engine):
        """获取引擎支持的扩展名"""
        supported_exts = []

        try:
            # 方式1：检查supported_extensions属性
            if hasattr(engine, 'supported_extensions'):
                exts = engine.supported_extensions
                if isinstance(exts, list):
                    supported_exts.extend(exts)

            # 方式2：通过supports方法测试常见扩展名
            if hasattr(engine, 'supports'):
                common_extensions = ['pdf', 'docx', 'xlsx', 'pptx', 'txt', 'jpg', 'png']
                for ext in common_extensions:
                    try:
                        if engine.supports(ext):
                            supported_exts.append(ext)
                    except:
                        pass
        except Exception as e:
            logger.warning(f"获取引擎支持的扩展名失败: {e}")

        # 去重
        return list(set(supported_exts))

    async def parse_document(
        self,
        file_path: Path,
        file_type: FileType,
        request: ParseRequest,
        request_id: str
    ) -> ParseResult:
        """解析文档

        Args:
            file_path: 文件路径
            file_type: 文件类型
            request: 解析请求参数
            request_id: 请求ID

        Returns:
            ParseResult: 解析结果

        Raises:
            DocumentParserError: 解析失败
        """
        logger.info(
            "开始解析文档",
            extra={
                "request_id": request_id,
                "file_path": str(file_path),
                "file_type": file_type.value,
                "strategy": request.strategy.value,
            }
        )

        try:
            # 根据文件类型选择解析引擎
            engine = self._get_engine(file_type)
            if not engine:
                raise UnsupportedFormatError(file_type.value)

            # 调用引擎解析文档
            parse_result = await engine.parse(file_path, request, request_id)

            logger.info(
                "文档解析完成",
                extra={
                    "request_id": request_id,
                    "file_type": file_type.value,
                    "pages_count": parse_result.document_metadata.page_count,
                }
            )

            return parse_result

        except UnsupportedFormatError:
            raise
        except Exception as exc:
            logger.error(
                "文档解析失败",
                extra={
                    "request_id": request_id,
                    "file_path": str(file_path),
                    "error": str(exc),
                },
                exc_info=exc
            )
            raise ParsingFailedError(
                f"文档解析失败: {str(exc)}",
                details={"file_type": file_type.value}
            )

    def _get_engine(self, file_type: FileType):
        """根据文件类型获取解析引擎"""
        # 获取文件扩展名
        extension = file_type.value.lower()

        # 查找支持该扩展名的引擎
        if extension not in self._engines:
            logger.warning(f"没有找到支持 {extension} 的引擎")
            return None

        engines = self._engines[extension]

        # 根据优先级选择引擎（优先级高的先尝试）
        # 这里需要引擎有get_priority方法
        sorted_engines = sorted(
            engines,
            key=lambda e: getattr(e, 'get_priority', lambda: 0)(),
            reverse=True
        )

        if sorted_engines:
            selected_engine = sorted_engines[0]
            logger.debug(
                f"为 {extension} 选择引擎: {selected_engine.name}",
                extra={
                    "extension": extension,
                    "engine": selected_engine.name,
                    "priority": getattr(selected_engine, 'get_priority', lambda: 0)()
                }
            )
            return selected_engine

        return None

    def _create_mock_result(self, file_type: FileType) -> ParseResult:
        """创建模拟结果（用于测试）"""
        # 创建文档元数据
        metadata = DocumentMetadata(
            title="测试文档",
            author="测试作者",
            subject="测试主题",
            keywords=["测试", "文档"],
            created_date="2024-01-01T00:00:00",
            modified_date="2024-01-01T00:00:00",
            page_count=3
        )

        # 创建页面内容
        pages = []
        for i in range(3):
            page_content = PageContent(
                page=i + 1,
                text=f"这是第{i+1}页的测试文本内容。这是用于测试的文档解析服务。",
                tables=[
                    Table(
                        table_index=0,
                        rows=[
                            ["标题1", "标题2", "标题3"],
                            ["数据1", "数据2", "数据3"],
                            ["数据4", "数据5", "数据6"]
                        ],
                        bbox=None
                    )
                ] if i == 0 else [],
                images=[
                    ImageInfo(
                        image_index=0,
                        description="测试图片描述",
                        bbox=[0.1, 0.4, 0.9, 0.8]
                    )
                ] if i == 1 else [],
                metadata={
                    "dimensions": {"width": 595.0, "height": 842.0},
                    "rotation": 0
                }
            )
            pages.append(page_content)

        # 创建文档结构
        structure = {
            "headings": [
                {
                    "level": 1,
                    "text": "第一章 测试标题",
                    "page": 1,
                    "bbox": [0.1, 0.1, 0.9, 0.2]
                },
                {
                    "level": 2,
                    "text": "1.1 测试子标题",
                    "page": 1,
                    "bbox": [0.1, 0.3, 0.9, 0.35]
                }
            ],
            "paragraphs": [
                {
                    "text": "这是一个测试段落。",
                    "page": 1,
                    "bbox": [0.1, 0.4, 0.9, 0.45]
                }
            ]
        }

        # 创建解析结果
        result = ParseResult(
            content=pages,
            document_metadata=metadata,
            structure=structure,
            ocr_triggered=False
        )

        return result
"""Office文档解析引擎"""
import asyncio
from pathlib import Path
import shutil
import subprocess
import tempfile
import traceback

from rag_document_parser.engines.base import BaseParserEngine
from rag_document_parser.models.schemas import FileType, ParseRequest, ParseResult
from rag_document_parser.services.config import settings
from rag_document_parser.services.exceptions import (
    DependencyError,
    OfficeConversionError,
    ParsingFailedError,
    UnsupportedFormatError,
)

try:
    from .excel_processor import ExcelDocumentProcessor
    from .presentation_processor import PowerPointDocumentProcessor
    from .word_processor import WordDocumentProcessor

    OFFICE_AVAILABLE = True
except ImportError:  # pragma: no cover - 依赖缺失时在运行期处理
    OFFICE_AVAILABLE = False

from .config import OfficeEngineConfig


class OfficeEngine(BaseParserEngine):
    """Office文档解析引擎。"""

    LEGACY_EXTENSION_MAP = {"doc": "docx", "xls": "xlsx", "ppt": "pptx"}
    FILE_TYPE_MAP = {
        "doc": FileType.DOC,
        "docx": FileType.DOCX,
        "xls": FileType.XLS,
        "xlsx": FileType.XLSX,
        "ppt": FileType.PPT,
        "pptx": FileType.PPTX,
    }

    def __init__(self, config: OfficeEngineConfig | None = None) -> None:
        super().__init__("office")
        self.config = config or OfficeEngineConfig()
        self._processors: dict[str, object] = {}
        self._initialize_processors()
        self.supported_extensions = sorted(set(self._processors).union(self.LEGACY_EXTENSION_MAP))

    def _initialize_processors(self) -> None:
        if not OFFICE_AVAILABLE:
            self.log_warning("Office依赖未安装，Office引擎将处于禁用状态")
            return

        self._processors["docx"] = WordDocumentProcessor()
        self._processors["xlsx"] = ExcelDocumentProcessor()
        self._processors["pptx"] = PowerPointDocumentProcessor()

    async def parse(self, file_path: Path, request: ParseRequest, request_id: str) -> ParseResult:
        self.log_info(
            "开始解析Office文档",
            extra={"request_id": request_id, "file_path": str(file_path)},
        )

        if not OFFICE_AVAILABLE:
            raise UnsupportedFormatError(
                "office",
                details={
                    "reason": "Office依赖未安装",
                    "required_deps": ["python-docx", "openpyxl", "python-pptx"],
                },
            )

        loop = asyncio.get_running_loop()
        try:
            result = await loop.run_in_executor(None, self._parse_sync, file_path, request, request_id)
            self.log_info(
                "Office文档解析完成",
                extra={
                    "request_id": request_id,
                    "file_path": str(file_path),
                    "pages_count": result.document_metadata.page_count,
                },
            )
            return result
        except Exception as exc:
            self.log_error(
                "Office文档解析失败",
                extra={
                    "request_id": request_id,
                    "file_path": str(file_path),
                    "error": str(exc),
                    "traceback": traceback.format_exc(),
                },
            )
            raise ParsingFailedError(
                f"Office文档解析失败: {str(exc)}",
                details={"engine": self.name, "file_path": str(file_path)},
            ) from exc

    def _parse_sync(self, file_path: Path, request: ParseRequest, request_id: str) -> ParseResult:
        if not file_path.exists():
            raise FileNotFoundError(str(file_path))

        source_extension = file_path.suffix.lower().lstrip(".")
        current_path = file_path
        cleanup_dir: Path | None = None

        try:
            extension = source_extension
            if extension in self.LEGACY_EXTENSION_MAP:
                current_path, cleanup_dir = self._convert_legacy_document(current_path, extension, request_id)
                extension = current_path.suffix.lower().lstrip(".")

            processor = self._processors.get(extension)
            if processor is None:
                raise UnsupportedFormatError(
                    extension,
                    details={
                        "supported_extensions": self.supported_extensions,
                        "file_path": str(file_path),
                    },
                )

            if not hasattr(processor, "_parse_sync"):
                raise NotImplementedError(f"处理器 {type(processor).__name__} 未实现_parse_sync")

            result = processor._parse_sync(current_path, request, request_id)
            result.document_metadata.file_type = self.FILE_TYPE_MAP.get(source_extension, result.document_metadata.file_type)
            return result
        finally:
            if cleanup_dir and cleanup_dir.exists():
                shutil.rmtree(cleanup_dir, ignore_errors=True)

    def _convert_legacy_document(self, file_path: Path, extension: str, request_id: str) -> tuple[Path, Path]:
        if not settings.ENABLE_OFFICE_CONVERSION:
            raise OfficeConversionError(
                "旧版Office转换未启用",
                details={"file_path": str(file_path), "extension": extension},
            )

        soffice_path = settings.get_libreoffice_path()
        if not soffice_path:
            raise DependencyError("LibreOffice", "未找到LibreOffice，无法转换旧版Office文档")

        target_extension = self.LEGACY_EXTENSION_MAP[extension]
        temp_dir = Path(tempfile.mkdtemp(prefix="office-convert-", dir=settings.TEMP_DIR))
        command = [
            str(soffice_path),
            "--headless",
            "--convert-to",
            target_extension,
            "--outdir",
            str(temp_dir),
            str(file_path),
        ]

        try:
            result = subprocess.run(
                command,
                capture_output=True,
                text=True,
                timeout=settings.CONVERSION_TIMEOUT,
                check=False,
            )
        except subprocess.TimeoutExpired as exc:
            raise OfficeConversionError(
                "LibreOffice转换超时",
                details={"command": command, "timeout": settings.CONVERSION_TIMEOUT},
            ) from exc

        if result.returncode != 0:
            raise OfficeConversionError(
                "LibreOffice转换失败",
                details={
                    "command": command,
                    "stdout": result.stdout,
                    "stderr": result.stderr,
                },
            )

        converted_path = temp_dir / f"{file_path.stem}.{target_extension}"
        if not converted_path.exists() or converted_path.stat().st_size == 0:
            raise OfficeConversionError(
                "转换结果不存在或为空",
                details={"converted_path": str(converted_path), "stdout": result.stdout},
            )

        self.log_info(
            "旧版Office文档转换成功",
            extra={
                "request_id": request_id,
                "source": str(file_path),
                "converted": str(converted_path),
            },
        )
        return converted_path, temp_dir

    def supports(self, file_extension: str) -> bool:
        return file_extension.lower() in self.supported_extensions

    def get_priority(self) -> int:
        return 70

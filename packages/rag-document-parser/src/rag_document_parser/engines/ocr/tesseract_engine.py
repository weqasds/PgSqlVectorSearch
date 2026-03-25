"""Tesseract OCR引擎"""
import asyncio
from datetime import datetime
from pathlib import Path
import traceback
from typing import Any

try:
    import pytesseract
    from PIL import Image, ImageEnhance, ImageFilter

    TESSERACT_AVAILABLE = True
except ImportError:  # pragma: no cover - 依赖缺失时仅在运行期使用
    TESSERACT_AVAILABLE = False

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
from rag_document_parser.services.config import settings
from rag_document_parser.services.exceptions import DependencyError, ParsingFailedError

from .config import OCRConfig


class TesseractEngine(BaseParserEngine):
    """基于Tesseract的OCR引擎。"""

    def __init__(self, config: OCRConfig | None = None):
        super().__init__("tesseract")
        self.config = config or OCRConfig(timeout=settings.OCR_TIMEOUT)
        self.supported_extensions = [
            "png",
            "jpg",
            "jpeg",
            "tiff",
            "tif",
            "bmp",
            "gif",
            "webp",
            "ppm",
            "pgm",
            "pbm",
            "pnm",
        ]
        self._verify_tesseract()

    def _verify_tesseract(self) -> None:
        if not TESSERACT_AVAILABLE:
            self.log_warning("Tesseract依赖未安装，OCR引擎将处于禁用状态")
            return

        tesseract_path = settings.get_tesseract_path()
        if tesseract_path:
            pytesseract.pytesseract.tesseract_cmd = str(tesseract_path)

        try:
            version = pytesseract.get_tesseract_version()
            self.log_info(f"Tesseract版本: {version}")
        except Exception as exc:
            self.log_warning(f"Tesseract验证失败: {exc}")

    async def parse(self, file_path: Path, request: ParseRequest, request_id: str) -> ParseResult:
        if not TESSERACT_AVAILABLE:
            raise DependencyError("pytesseract", "Tesseract OCR引擎未安装")

        try:
            with Image.open(file_path) as image:
                file_type = self._get_file_type_from_extension(file_path.suffix.lstrip("."))
                return await self.parse_image(
                    image=image,
                    request=request,
                    request_id=request_id,
                    file_type=file_type,
                    source_name=file_path.name,
                    file_path=file_path,
                )
        except Exception as exc:
            self.log_error(
                "OCR解析失败",
                extra={"request_id": request_id, "file_path": str(file_path), "error": str(exc), "traceback": traceback.format_exc()},
            )
            raise ParsingFailedError(
                f"OCR解析失败: {str(exc)}",
                details={"engine": self.name, "file_path": str(file_path)},
            ) from exc

    async def parse_image(
        self,
        image: "Image.Image",
        request: ParseRequest,
        request_id: str,
        file_type: FileType = FileType.PNG,
        source_name: str = "image",
        file_path: Path | None = None,
    ) -> ParseResult:
        """直接解析已加载的图像对象。"""
        if not TESSERACT_AVAILABLE:
            raise DependencyError("pytesseract", "Tesseract OCR引擎未安装")

        image_copy = image.copy()
        loop = asyncio.get_running_loop()
        return await loop.run_in_executor(
            None,
            self._parse_loaded_image,
            image_copy,
            request,
            request_id,
            file_type,
            source_name,
            file_path,
        )

    def _parse_loaded_image(
        self,
        image: "Image.Image",
        request: ParseRequest,
        request_id: str,
        file_type: FileType,
        source_name: str,
        file_path: Path | None,
    ) -> ParseResult:
        try:
            working_image = image
            if self.config.preprocess_image:
                working_image = self._preprocess_image(image)

            ocr_data = self._perform_ocr(working_image)
            metadata = self._extract_metadata(source_name, working_image, file_type, file_path)
            page_content = self._extract_page_content(ocr_data, working_image, request)
            structure = self._extract_structure(ocr_data)
            return ParseResult(
                content=[page_content],
                document_metadata=metadata,
                structure=structure,
                ocr_triggered=True,
            )
        finally:
            image.close()
            if working_image is not image:
                working_image.close()

    def _preprocess_image(self, image: "Image.Image") -> "Image.Image":
        try:
            working = image.convert("L") if image.mode != "L" else image.copy()
            working = ImageEnhance.Contrast(working).enhance(1.5)
            working = ImageEnhance.Brightness(working).enhance(1.2)
            working = working.filter(ImageFilter.SHARPEN)
            working = working.filter(ImageFilter.MedianFilter(size=3))
            return working
        except Exception as exc:
            self.log_warning(f"图像预处理失败: {exc}")
            return image.copy()

    def _perform_ocr(self, image: "Image.Image") -> dict[str, Any]:
        custom_config = self._build_tesseract_config()
        return pytesseract.image_to_data(
            image,
            lang="+".join(self.config.languages),
            config=custom_config,
            output_type=pytesseract.Output.DICT,
            timeout=self.config.timeout,
        )

    def _build_tesseract_config(self) -> str:
        config_parts = [f"--psm {self.config.page_seg_mode}", f"--oem {self.config.ocr_engine_mode}"]
        if self.config.preserve_interword_spaces:
            config_parts.append("-c preserve_interword_spaces=1")
        return " ".join(config_parts)

    def _extract_metadata(
        self,
        source_name: str,
        image: "Image.Image",
        file_type: FileType,
        file_path: Path | None,
    ) -> DocumentMetadata:
        file_size = file_path.stat().st_size if file_path and file_path.exists() else None
        created_date = datetime.fromtimestamp(file_path.stat().st_ctime) if file_path and file_path.exists() else None
        modified_date = datetime.fromtimestamp(file_path.stat().st_mtime) if file_path and file_path.exists() else None
        return DocumentMetadata(
            title=Path(source_name).stem,
            author=None,
            subject=None,
            keywords=[],
            created_date=created_date,
            modified_date=modified_date,
            page_count=1,
            file_size=file_size,
            file_type=file_type,
        )

    def _get_file_type_from_extension(self, extension: str) -> FileType:
        extension_map = {
            "png": FileType.PNG,
            "jpg": FileType.JPG,
            "jpeg": FileType.JPEG,
            "tiff": FileType.TIFF,
            "tif": FileType.TIF,
            "bmp": FileType.BMP,
            "gif": FileType.GIF,
            "webp": FileType.WEBP,
            "ppm": FileType.PPM,
            "pgm": FileType.PGM,
            "pbm": FileType.PBM,
            "pnm": FileType.PNM,
            "pdf": FileType.PDF,
            "doc": FileType.DOC,
            "docx": FileType.DOCX,
            "xls": FileType.XLS,
            "xlsx": FileType.XLSX,
            "ppt": FileType.PPT,
            "pptx": FileType.PPTX,
            "txt": FileType.TXT,
        }
        return extension_map.get(extension.lower(), FileType.UNKNOWN)

    def _extract_page_content(
        self,
        ocr_data: dict[str, Any],
        image: "Image.Image",
        request: ParseRequest,
    ) -> PageContent:
        text = self._extract_text_from_ocr(ocr_data)
        tables = self._extract_tables_from_ocr(ocr_data) if request.extract_tables else []
        images = self._extract_image_info(image) if request.extract_images else []
        return PageContent(
            page=1,
            text=text,
            tables=tables,
            images=images,
            metadata=PageMetadata(
                dimensions={"width": float(image.width), "height": float(image.height)},
                rotation=0,
                page_number=1,
            ),
        )

    def _extract_text_from_ocr(self, ocr_data: dict[str, Any]) -> str:
        grouped_lines: dict[tuple[int, int, int], list[str]] = {}
        for index, raw_text in enumerate(ocr_data.get("text", [])):
            text = str(raw_text).strip()
            if not text:
                continue
            key = (
                int(ocr_data.get("block_num", [0])[index]),
                int(ocr_data.get("par_num", [0])[index]),
                int(ocr_data.get("line_num", [index])[index]),
            )
            grouped_lines.setdefault(key, []).append(text)

        return "\n".join(" ".join(parts) for _, parts in sorted(grouped_lines.items()))

    def _extract_tables_from_ocr(self, ocr_data: dict[str, Any]) -> list[Table]:
        rows_by_y: dict[int, list[tuple[int, str]]] = {}
        for index, raw_text in enumerate(ocr_data.get("text", [])):
            text = str(raw_text).strip()
            if not text:
                continue
            if int(ocr_data.get("conf", [0])[index]) < 0:
                continue
            top = int(ocr_data.get("top", [0])[index])
            left = int(ocr_data.get("left", [0])[index])
            y_bucket = round(top / 12) * 12
            rows_by_y.setdefault(y_bucket, []).append((left, text))

        table_rows: list[list[str]] = []
        for _, columns in sorted(rows_by_y.items()):
            columns.sort(key=lambda item: item[0])
            if len(columns) >= 2:
                table_rows.append([text for _, text in columns])

        if len(table_rows) < 2:
            return []

        return [Table(table_index=0, rows=table_rows, bbox=None)]

    def _extract_image_info(self, image: "Image.Image") -> list[ImageInfo]:
        return [
            ImageInfo(
                image_index=0,
                description="原始图像",
                bbox=BoundingBox(x1=0, y1=0, x2=float(image.width), y2=float(image.height)),
            )
        ]

    def _extract_structure(self, ocr_data: dict[str, Any]) -> DocumentStructure:
        paragraphs: list[Paragraph] = []
        headings: list[Heading] = []
        for index, raw_text in enumerate(ocr_data.get("text", [])):
            text = str(raw_text).strip()
            if not text:
                continue
            height = int(ocr_data.get("height", [0])[index])
            left = float(ocr_data.get("left", [0])[index])
            top = float(ocr_data.get("top", [0])[index])
            width = float(ocr_data.get("width", [0])[index])
            bbox = BoundingBox(x1=left, y1=top, x2=left + width, y2=top + float(height))
            if height >= 24:
                headings.append(Heading(level=1, text=text, page=1, bbox=bbox))
            else:
                paragraphs.append(Paragraph(text=text, page=1, bbox=bbox))

        return DocumentStructure(headings=headings, paragraphs=paragraphs)

    def supports(self, file_extension: str) -> bool:
        return file_extension.lower() in self.supported_extensions

    def get_priority(self) -> int:
        return 80

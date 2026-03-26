"""解析服务 - 协调文档解析流程"""
import asyncio
from io import BytesIO
from pathlib import Path

try:
    import fitz
except ImportError:  # pragma: no cover - 依赖缺失时在运行期处理
    fitz = None

try:
    from PIL import Image
except ImportError:  # pragma: no cover - 依赖缺失时在运行期处理
    Image = None

from rag_document_parser.models.schemas import (
    DocumentMetadata,
    DocumentStructure,
    FileType,
    PageContent,
    ParseRequest,
    ParseResult,
    ParseStrategy,
)
from rag_document_parser.services.config import settings
from rag_document_parser.services.exceptions import (
    DependencyError,
    DocumentParserError,
    ParsingFailedError,
    UnsupportedFormatError,
)
from rag_document_parser.services.exceptions import (
    TimeoutError as ParserTimeoutError,
)
from rag_document_parser.utils.logging import get_logger

logger = get_logger(__name__)


class ParserService:
    """文档解析服务（协调各个解析引擎）。"""

    PDF_FILE_TYPES = {FileType.PDF}
    OFFICE_FILE_TYPES = {
        FileType.DOC,
        FileType.DOCX,
        FileType.XLS,
        FileType.XLSX,
        FileType.PPT,
        FileType.PPTX,
    }
    IMAGE_FILE_TYPES = {
        FileType.PNG,
        FileType.JPG,
        FileType.JPEG,
        FileType.TIF,
        FileType.TIFF,
        FileType.BMP,
        FileType.GIF,
        FileType.WEBP,
        FileType.PPM,
        FileType.PGM,
        FileType.PBM,
        FileType.PNM,
    }
    TEXT_FILE_TYPES = {FileType.TXT}

    def __init__(self) -> None:
        self._engines_by_extension: dict[str, list[object]] = {}
        self._engines_by_name: dict[str, object] = {}
        self._semaphore = asyncio.Semaphore(max(1, settings.MAX_CONCURRENT_TASKS))
        self._setup_engines()

    def _setup_engines(self) -> None:
        engine_factories = [
            ("PDF", lambda: __import__("rag_document_parser.engines.pdf", fromlist=["PyMuPDFEngine", "PDFPlumberEngine"])),
            ("OCR", lambda: __import__("rag_document_parser.engines.ocr", fromlist=["TesseractEngine"])),
            ("Office", lambda: __import__("rag_document_parser.engines.office", fromlist=["OfficeEngine"])),
            ("Text", lambda: __import__("rag_document_parser.engines.text", fromlist=["SimpleTextEngine"])),
        ]

        for engine_group, importer in engine_factories:
            try:
                module = importer()
                if engine_group == "PDF":
                    self._register_engine(module.PyMuPDFEngine())
                    self._register_engine(module.PDFPlumberEngine())
                elif engine_group == "OCR":
                    self._register_engine(module.TesseractEngine())
                elif engine_group == "Office":
                    self._register_engine(module.OfficeEngine())
                else:
                    self._register_engine(module.SimpleTextEngine())
                logger.info(f"{engine_group}引擎注册成功")
            except Exception as exc:
                logger.warning(f"{engine_group}引擎导入失败: {exc}")

        logger.info(f"引擎初始化完成，已注册 {len(self._engines_by_name)} 个引擎")

    def _register_engine(self, engine: object) -> None:
        engine_name = engine.name
        self._engines_by_name[engine_name] = engine
        for extension in self._get_supported_extensions(engine):
            self._engines_by_extension.setdefault(extension, []).append(engine)
        logger.debug(f"注册引擎: {engine_name}")

    def _get_supported_extensions(self, engine: object) -> list[str]:
        supported = getattr(engine, "supported_extensions", None)
        if isinstance(supported, list):
            return list(dict.fromkeys(str(item).lower() for item in supported))
        return []

    async def parse_document(
        self,
        file_path: Path,
        file_type: FileType,
        request: ParseRequest,
        request_id: str,
    ) -> ParseResult:
        normalized_request = self._normalize_request(request)
        timeout = normalized_request.timeout or settings.TASK_TIMEOUT

        logger.info(
            "开始解析文档",
            extra={
                "request_id": request_id,
                "file_path": str(file_path),
                "file_type": file_type.value,
                "strategy": normalized_request.strategy.value,
            },
        )

        try:
            async with self._semaphore:
                result = await asyncio.wait_for(
                    self._parse_document_impl(file_path, file_type, normalized_request, request_id),
                    timeout=timeout,
                )
        except asyncio.TimeoutError as exc:
            raise ParserTimeoutError(
                "文档解析",
                timeout,
                details={"file_path": str(file_path), "file_type": file_type.value},
            ) from exc
        except DocumentParserError:
            raise
        except Exception as exc:
            logger.error(
                "文档解析失败",
                extra={
                    "request_id": request_id,
                    "file_path": str(file_path),
                    "file_type": file_type.value,
                    "error": str(exc),
                },
                exc_info=exc,
            )
            raise ParsingFailedError(
                f"文档解析失败: {str(exc)}",
                details={"file_type": file_type.value, "file_path": str(file_path)},
            ) from exc

        logger.info(
            "文档解析完成",
            extra={
                "request_id": request_id,
                "file_type": file_type.value,
                "pages_count": result.document_metadata.page_count,
                "ocr_triggered": result.ocr_triggered,
            },
        )
        return result

    async def _parse_document_impl(
        self,
        file_path: Path,
        file_type: FileType,
        request: ParseRequest,
        request_id: str,
    ) -> ParseResult:
        if file_type in self.PDF_FILE_TYPES:
            result = await self._parse_pdf(file_path, request, request_id)
        elif file_type in self.OFFICE_FILE_TYPES:
            result = await self._parse_with_extension(file_type.value, file_path, request, request_id)
        elif file_type in self.IMAGE_FILE_TYPES:
            if not settings.ENABLE_OCR:
                raise UnsupportedFormatError(
                    file_type.value,
                    details={"reason": "当前服务未启用OCR"},
                )
            result = await self._parse_with_extension(file_type.value, file_path, request, request_id)
        elif file_type in self.TEXT_FILE_TYPES:
            result = await self._parse_with_extension(file_type.value, file_path, request, request_id)
        else:
            raise UnsupportedFormatError(file_type.value)

        result.document_metadata.file_type = file_type
        result.document_metadata.page_count = max(1, len(result.content))
        return self._post_process_result(result, request)

    async def _parse_with_extension(
        self,
        extension: str,
        file_path: Path,
        request: ParseRequest,
        request_id: str,
    ) -> ParseResult:
        engine = self._get_best_engine(extension)
        if not engine:
            raise UnsupportedFormatError(extension)
        return await engine.parse(file_path, request, request_id)

    async def _parse_pdf(
        self,
        file_path: Path,
        request: ParseRequest,
        request_id: str,
    ) -> ParseResult:
        if request.strategy == ParseStrategy.OCR_ONLY:
            return await self._parse_pdf_with_ocr(file_path, request, request_id)

        primary_engine = self._resolve_pdf_primary_engine()
        if not primary_engine:
            raise UnsupportedFormatError("pdf")

        result = await primary_engine.parse(file_path, request, request_id)

        supplement_engine = self._resolve_pdf_supplement_engine(primary_engine)
        if request.extract_tables and supplement_engine is not None:
            supplement_result = await supplement_engine.parse(file_path, request, request_id)
            result = self._merge_pdf_tables(result, supplement_result)

        ocr_pages = self._select_pdf_ocr_pages(result, request)
        if ocr_pages:
            try:
                result = await self._augment_pdf_with_ocr(
                    file_path=file_path,
                    base_result=result,
                    request=request,
                    request_id=request_id,
                    page_indexes=ocr_pages,
                )
            except Exception as exc:
                logger.warning(
                    "PDF OCR回退失败，保留主解析结果",
                    extra={"request_id": request_id, "file_path": str(file_path), "error": str(exc)},
                )

        return result

    def _resolve_pdf_primary_engine(self):
        preferred = settings.PDF_ENGINE.lower()
        if preferred in self._engines_by_name:
            return self._engines_by_name[preferred]
        return self._get_best_engine("pdf")

    def _resolve_pdf_supplement_engine(self, primary_engine: object):
        supplement = self._engines_by_name.get("pdfplumber")
        if supplement is not None and supplement is not primary_engine:
            return supplement
        return None

    def _get_best_engine(self, extension: str):
        engines = self._engines_by_extension.get(extension.lower(), [])
        if not engines:
            logger.warning(f"没有找到支持 {extension} 的引擎")
            return None
        return sorted(
            engines,
            key=lambda item: getattr(item, "get_priority", lambda: 0)(),
            reverse=True,
        )[0]

    def _normalize_request(self, request: ParseRequest) -> ParseRequest:
        updates: dict[str, object] = {
            "ocr_threshold": request.ocr_threshold if request.ocr_threshold is not None else settings.OCR_THRESHOLD,
            "timeout": request.timeout if request.timeout is not None else settings.TASK_TIMEOUT,
        }

        if request.strategy == ParseStrategy.TEXT_ONLY:
            updates["extract_tables"] = False
            updates["extract_images"] = False
        elif request.strategy == ParseStrategy.TEXT_AND_TABLES:
            updates["extract_tables"] = True
            updates["extract_images"] = False
        elif request.strategy == ParseStrategy.TABLES_ONLY:
            updates["extract_tables"] = True
            updates["extract_images"] = False

        return request.model_copy(update=updates)

    def _merge_pdf_tables(self, primary: ParseResult, supplement: ParseResult) -> ParseResult:
        for index, page in enumerate(primary.content):
            if index >= len(supplement.content):
                continue
            supplement_page = supplement.content[index]
            page.tables = self._merge_tables(page.tables, supplement_page.tables)
        return primary

    def _merge_tables(self, current_tables, extra_tables):
        merged = list(current_tables)
        seen = {tuple(tuple(row) for row in table.rows) for table in current_tables}
        for table in extra_tables:
            signature = tuple(tuple(row) for row in table.rows)
            if signature in seen:
                continue
            merged.append(table)
            seen.add(signature)
        return merged

    def _select_pdf_ocr_pages(self, result: ParseResult, request: ParseRequest) -> list[int]:
        if (
            not settings.ENABLE_OCR
            or "tesseract" not in self._engines_by_name
            or settings.get_tesseract_path() is None
        ):
            return []

        threshold = request.ocr_threshold or settings.OCR_THRESHOLD
        page_indexes: list[int] = []
        for index, page in enumerate(result.content):
            if len(page.text.strip()) <= threshold:
                page_indexes.append(index)
        return page_indexes

    async def _augment_pdf_with_ocr(
        self,
        file_path: Path,
        base_result: ParseResult,
        request: ParseRequest,
        request_id: str,
        page_indexes: list[int],
    ) -> ParseResult:
        ocr_engine = self._engines_by_name.get("tesseract")
        if ocr_engine is None:
            return base_result
        if fitz is None or Image is None:
            raise DependencyError("PyMuPDF/Pillow", "PDF OCR回退需要PyMuPDF和Pillow")

        with fitz.open(file_path) as document:
            for page_index in page_indexes:
                page_image = self._render_pdf_page(document.load_page(page_index))
                ocr_result = await ocr_engine.parse_image(
                    image=page_image,
                    request=request,
                    request_id=request_id,
                    file_type=FileType.PDF,
                    source_name=f"{file_path.stem}-page-{page_index + 1}.png",
                )
                base_result.content[page_index] = self._merge_page_content(
                    base_result.content[page_index],
                    ocr_result.content[0],
                    request,
                )
                self._merge_structure(base_result.structure, ocr_result.structure, page_index + 1)
                base_result.ocr_triggered = True
        return base_result

    async def _parse_pdf_with_ocr(
        self,
        file_path: Path,
        request: ParseRequest,
        request_id: str,
    ) -> ParseResult:
        ocr_engine = self._engines_by_name.get("tesseract")
        if ocr_engine is None:
            raise DependencyError("tesseract", "OCR_ONLY策略需要OCR引擎")
        if fitz is None or Image is None:
            raise DependencyError("PyMuPDF/Pillow", "PDF OCR解析需要PyMuPDF和Pillow")

        pages: list[PageContent] = []
        structure = DocumentStructure()
        with fitz.open(file_path) as document:
            metadata = self._build_pdf_metadata(document, file_path)
            for page_index, page in enumerate(document):
                page_image = self._render_pdf_page(page)
                ocr_result = await ocr_engine.parse_image(
                    image=page_image,
                    request=request,
                    request_id=request_id,
                    file_type=FileType.PDF,
                    source_name=f"{file_path.stem}-page-{page_index + 1}.png",
                )
                ocr_page = ocr_result.content[0]
                ocr_page.page = page_index + 1
                ocr_page.metadata.page_number = page_index + 1
                pages.append(ocr_page)
                self._merge_structure(structure, ocr_result.structure, page_index + 1)

        return ParseResult(
            content=pages,
            document_metadata=metadata,
            structure=structure,
            ocr_triggered=True,
        )

    def _build_pdf_metadata(self, document: "fitz.Document", file_path: Path) -> DocumentMetadata:
        metadata = document.metadata or {}
        keywords = metadata.get("keywords") or metadata.get("Keywords") or ""
        return DocumentMetadata(
            title=metadata.get("title") or metadata.get("Title") or file_path.stem,
            author=metadata.get("author") or metadata.get("Author"),
            subject=metadata.get("subject") or metadata.get("Subject"),
            keywords=[item.strip() for item in str(keywords).split(",") if item.strip()],
            created_date=None,
            modified_date=None,
            page_count=len(document),
            file_size=file_path.stat().st_size if file_path.exists() else None,
            file_type=FileType.PDF,
        )

    def _render_pdf_page(self, page: "fitz.Page"):
        pixmap = page.get_pixmap(matrix=fitz.Matrix(2, 2), alpha=False)
        buffer = BytesIO(pixmap.tobytes("png"))
        image = Image.open(buffer)
        image.load()
        buffer.close()
        return image

    def _merge_page_content(
        self,
        current_page: PageContent,
        ocr_page: PageContent,
        request: ParseRequest,
    ) -> PageContent:
        current_text = current_page.text.strip()
        ocr_text = ocr_page.text.strip()
        merged_text = ocr_text if len(ocr_text) > len(current_text) else current_page.text
        if not current_text:
            merged_text = ocr_page.text

        merged_tables = self._merge_tables(current_page.tables, ocr_page.tables) if request.extract_tables else []
        merged_images = (current_page.images or ocr_page.images) if request.extract_images else []

        return PageContent(
            page=current_page.page,
            text=merged_text,
            tables=merged_tables,
            images=merged_images,
            metadata=current_page.metadata.model_copy(deep=True),
        )

    def _merge_structure(
        self,
        target: DocumentStructure,
        source: DocumentStructure,
        page_number: int,
    ) -> None:
        existing_headings = {(item.level, item.text, item.page) for item in target.headings}
        for heading in source.headings:
            merged_heading = heading.model_copy(update={"page": page_number})
            key = (merged_heading.level, merged_heading.text, merged_heading.page)
            if key not in existing_headings:
                target.headings.append(merged_heading)
                existing_headings.add(key)

        existing_paragraphs = {(item.text, item.page) for item in target.paragraphs}
        for paragraph in source.paragraphs:
            merged_paragraph = paragraph.model_copy(update={"page": page_number})
            key = (merged_paragraph.text, merged_paragraph.page)
            if key not in existing_paragraphs:
                target.paragraphs.append(merged_paragraph)
                existing_paragraphs.add(key)

    def _post_process_result(self, result: ParseResult, request: ParseRequest) -> ParseResult:
        if not request.extract_tables:
            for page in result.content:
                page.tables = []
        if not request.extract_images:
            for page in result.content:
                page.images = []

        if request.strategy == ParseStrategy.TEXT_ONLY:
            for page in result.content:
                page.tables = []
                page.images = []
        elif request.strategy == ParseStrategy.TABLES_ONLY:
            for page in result.content:
                page.text = ""
                page.images = []
            result.structure = DocumentStructure()

        return result

    def _create_mock_result(self, file_type: FileType) -> ParseResult:
        return ParseResult(
            content=[
                PageContent(
                    page=1,
                    text="测试内容",
                    tables=[],
                    images=[],
                )
            ],
            document_metadata=DocumentMetadata(
                title="测试文档",
                author=None,
                subject=None,
                keywords=[],
                created_date=None,
                modified_date=None,
                page_count=1,
                file_size=None,
                file_type=file_type,
            ),
            structure=DocumentStructure(),
            ocr_triggered=False,
        )



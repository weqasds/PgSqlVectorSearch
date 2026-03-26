"""文件处理工具函数"""
import mimetypes
import uuid
from pathlib import Path
from typing import Any

from fastapi import UploadFile

from rag_document_parser.models.schemas import FileType
from rag_document_parser.services.exceptions import FileValidationError
from rag_document_parser.utils.logging import get_logger

logger = get_logger(__name__)

try:
    import magic

    MAGIC_AVAILABLE = True
except ImportError:
    MAGIC_AVAILABLE = False
    logger.warning("python-magic未安装，文件类型检测将仅使用扩展名和MIME类型")

SUPPORTED_FORMATS: dict[str, dict[str, Any]] = {
    "pdf": {
        "description": "便携式文档格式",
        "extensions": ["pdf"],
        "mime_types": ["application/pdf"],
    },
    "docx": {
        "description": "Microsoft Word文档",
        "extensions": ["docx"],
        "mime_types": [
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        ],
    },
    "doc": {
        "description": "旧版Microsoft Word文档",
        "extensions": ["doc"],
        "mime_types": ["application/msword"],
    },
    "xlsx": {
        "description": "Microsoft Excel工作簿",
        "extensions": ["xlsx"],
        "mime_types": [
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        ],
    },
    "xls": {
        "description": "旧版Microsoft Excel工作簿",
        "extensions": ["xls"],
        "mime_types": ["application/vnd.ms-excel"],
    },
    "pptx": {
        "description": "Microsoft PowerPoint演示文稿",
        "extensions": ["pptx"],
        "mime_types": [
            "application/vnd.openxmlformats-officedocument.presentationml.presentation"
        ],
    },
    "ppt": {
        "description": "旧版Microsoft PowerPoint演示文稿",
        "extensions": ["ppt"],
        "mime_types": ["application/vnd.ms-powerpoint"],
    },
    "txt": {
        "description": "纯文本文件",
        "extensions": ["txt"],
        "mime_types": ["text/plain"],
    },
    "png": {
        "description": "PNG图像",
        "extensions": ["png"],
        "mime_types": ["image/png"],
    },
    "jpeg": {
        "description": "JPEG图像",
        "extensions": ["jpeg", "jpg"],
        "mime_types": ["image/jpeg"],
    },
    "tiff": {
        "description": "TIFF图像",
        "extensions": ["tiff", "tif"],
        "mime_types": ["image/tiff"],
    },
    "bmp": {
        "description": "BMP图像",
        "extensions": ["bmp"],
        "mime_types": ["image/bmp"],
    },
    "gif": {
        "description": "GIF图像",
        "extensions": ["gif"],
        "mime_types": ["image/gif"],
    },
    "webp": {
        "description": "WebP图像",
        "extensions": ["webp"],
        "mime_types": ["image/webp"],
    },
}

_EXTENSION_MAP = {
    "pdf": FileType.PDF,
    "doc": FileType.DOC,
    "docx": FileType.DOCX,
    "xls": FileType.XLS,
    "xlsx": FileType.XLSX,
    "ppt": FileType.PPT,
    "pptx": FileType.PPTX,
    "txt": FileType.TXT,
    "png": FileType.PNG,
    "jpg": FileType.JPG,
    "jpeg": FileType.JPEG,
    "tif": FileType.TIF,
    "tiff": FileType.TIFF,
    "bmp": FileType.BMP,
    "gif": FileType.GIF,
    "webp": FileType.WEBP,
    "ppm": FileType.PPM,
    "pgm": FileType.PGM,
    "pbm": FileType.PBM,
    "pnm": FileType.PNM,
}

_MIME_MAP = {
    "application/pdf": FileType.PDF,
    "application/msword": FileType.DOC,
    "application/vnd.openxmlformats-officedocument.wordprocessingml.document": FileType.DOCX,
    "application/vnd.ms-excel": FileType.XLS,
    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet": FileType.XLSX,
    "application/vnd.ms-powerpoint": FileType.PPT,
    "application/vnd.openxmlformats-officedocument.presentationml.presentation": FileType.PPTX,
    "text/plain": FileType.TXT,
    "image/png": FileType.PNG,
    "image/jpeg": FileType.JPEG,
    "image/tiff": FileType.TIFF,
    "image/bmp": FileType.BMP,
    "image/gif": FileType.GIF,
    "image/webp": FileType.WEBP,
}


async def validate_file_size(file: UploadFile, max_size: int) -> None:
    """验证上传文件大小。"""
    file.file.seek(0, 2)
    file_size = file.file.tell()
    file.file.seek(0)

    if file_size > max_size:
        raise FileValidationError(
            f"文件大小超过限制: {file_size} > {max_size}",
            details={
                "file_size": file_size,
                "max_size": max_size,
                "max_size_mb": max_size / (1024 * 1024),
            },
        )

    logger.debug("文件大小验证通过", extra={"filename": file.filename, "size": file_size})


async def detect_file_type(file: UploadFile) -> FileType:
    """检测文件类型。"""
    filename = (file.filename or "").lower()
    extension = Path(filename).suffix.lstrip(".")
    content_type = file.content_type
    detected_mime: str | None = None

    try:
        file.file.seek(0)
        header = await file.read(4096)
        file.file.seek(0)

        if MAGIC_AVAILABLE and header:
            try:
                detected_mime = magic.Magic(mime=True).from_buffer(header)
            except Exception as exc:  # pragma: no cover - 依赖可选
                logger.warning("python-magic检测失败", extra={"error": str(exc)})

        file_type = _determine_file_type(extension, content_type, detected_mime, header)
        if file_type is FileType.UNKNOWN:
            raise FileValidationError(
                "无法识别的文件类型",
                details={
                    "filename": filename,
                    "extension": extension,
                    "content_type": content_type,
                    "detected_mime": detected_mime,
                },
            )

        return file_type
    except FileValidationError:
        raise
    except Exception as exc:
        logger.warning("文件类型检测失败", extra={"filename": filename, "error": str(exc)})
        raise FileValidationError(
            f"文件类型检测失败: {str(exc)}",
            details={"filename": filename},
        ) from exc


def _determine_file_type(
    extension: str,
    content_type: str | None,
    detected_mime: str | None,
    header: bytes | None,
) -> FileType:
    """根据扩展名、MIME类型和文件头判断文件类型。"""
    if extension in _EXTENSION_MAP:
        return _EXTENSION_MAP[extension]

    if content_type and content_type in _MIME_MAP:
        mime_type = _MIME_MAP[content_type]
        if mime_type is FileType.JPEG and extension == "jpg":
            return FileType.JPG
        return mime_type

    if detected_mime and detected_mime in _MIME_MAP:
        return _MIME_MAP[detected_mime]

    if header:
        if header.startswith(b"%PDF"):
            return FileType.PDF
        if header.startswith(b"\x89PNG\r\n\x1a\n"):
            return FileType.PNG
        if header.startswith(b"\xff\xd8\xff"):
            return FileType.JPEG
        if header.startswith((b"II*\x00", b"MM\x00*")):
            return FileType.TIFF
        if header.startswith(b"GIF8"):
            return FileType.GIF
        if header.startswith(b"BM"):
            return FileType.BMP
        if header.startswith(b"RIFF") and header[8:12] == b"WEBP":
            return FileType.WEBP

    guessed_mime, _ = mimetypes.guess_type(f"dummy.{extension}")
    if guessed_mime and guessed_mime in _MIME_MAP:
        return _MIME_MAP[guessed_mime]

    return FileType.UNKNOWN


def is_file_type_supported(file_type: FileType) -> bool:
    """检查文件类型是否支持。"""
    return file_type.value in SUPPORTED_FORMATS


async def save_upload_file(file: UploadFile, temp_dir: Path) -> Path:
    """保存上传文件到临时目录。"""
    temp_dir.mkdir(parents=True, exist_ok=True)

    safe_name = file.filename or "upload.bin"
    file_path = temp_dir / f"{uuid.uuid4()}_{safe_name}"

    try:
        import aiofiles

        async with aiofiles.open(file_path, "wb") as output:
            while chunk := await file.read(1024 * 1024):
                await output.write(chunk)

        file.file.seek(0)
        logger.debug("文件保存成功", extra={"path": str(file_path)})
        return file_path
    except Exception as exc:
        if file_path.exists():
            file_path.unlink(missing_ok=True)
        raise FileValidationError(
            f"文件保存失败: {str(exc)}",
            details={"filename": safe_name},
        ) from exc


def get_file_info(file_path: Path) -> dict[str, Any]:
    """获取文件信息。"""
    stat = file_path.stat()
    return {
        "path": str(file_path),
        "size": stat.st_size,
        "created": stat.st_ctime,
        "modified": stat.st_mtime,
        "name": file_path.name,
        "extension": file_path.suffix.lstrip("."),
    }

"""文件处理工具函数"""
import asyncio
import mimetypes
from pathlib import Path
from typing import Optional, Dict, Any, Set

from fastapi import UploadFile, HTTPException
from rag_document_parser.models.schemas import FileType
from rag_document_parser.services.exceptions import FileValidationError
from rag_document_parser.utils.logging import get_logger

logger = get_logger(__name__)

# 可选导入python-magic
try:
    import magic  # python-magic
    MAGIC_AVAILABLE = True
except ImportError:
    MAGIC_AVAILABLE = False
    logger.warning("python-magic未安装，文件类型检测将仅使用扩展名和MIME类型")

# 支持的格式映射
SUPPORTED_FORMATS = {
    "pdf": {
        "description": "便携式文档格式",
        "extensions": ["pdf"],
        "mime_types": ["application/pdf"]
    },
    "docx": {
        "description": "Microsoft Word文档",
        "extensions": ["docx"],
        "mime_types": ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"]
    },
    "doc": {
        "description": "旧版Microsoft Word文档",
        "extensions": ["doc"],
        "mime_types": ["application/msword"]
    },
    "xlsx": {
        "description": "Microsoft Excel工作簿",
        "extensions": ["xlsx"],
        "mime_types": ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"]
    },
    "xls": {
        "description": "旧版Microsoft Excel工作簿",
        "extensions": ["xls"],
        "mime_types": ["application/vnd.ms-excel"]
    },
    "pptx": {
        "description": "Microsoft PowerPoint演示文稿",
        "extensions": ["pptx"],
        "mime_types": ["application/vnd.openxmlformats-officedocument.presentationml.presentation"]
    },
    "ppt": {
        "description": "旧版Microsoft PowerPoint演示文稿",
        "extensions": ["ppt"],
        "mime_types": ["application/vnd.ms-powerpoint"]
    },
    "txt": {
        "description": "纯文本文件",
        "extensions": ["txt"],
        "mime_types": ["text/plain"]
    }
}


async def validate_file_size(file: UploadFile, max_size: int) -> None:
    """验证文件大小

    Args:
        file: 上传的文件
        max_size: 最大文件大小（字节）

    Raises:
        FileValidationError: 文件大小超过限制
    """
    # 获取文件大小
    file.file.seek(0, 2)  # 移动到文件末尾
    file_size = file.file.tell()
    file.file.seek(0)  # 重置文件指针

    if file_size > max_size:
        raise FileValidationError(
            f"文件大小超过限制: {file_size} > {max_size}",
            details={
                "file_size": file_size,
                "max_size": max_size,
                "max_size_mb": max_size / (1024 * 1024)
            }
        )

    logger.debug(f"文件大小验证通过: {file.filename}, size={file_size}")


async def detect_file_type(file: UploadFile) -> FileType:
    """检测文件类型

    Args:
        file: 上传的文件

    Returns:
        FileType: 检测到的文件类型

    Raises:
        FileValidationError: 文件类型检测失败
    """
    # 方法1: 通过文件扩展名
    filename = file.filename.lower()
    extension = Path(filename).suffix.lstrip('.')

    # 方法2: 通过MIME类型
    content_type = file.content_type

    # 方法3: 通过文件内容魔法字节（如果可用）
    file_content = None
    try:
        # 读取文件开头部分进行检测
        file.file.seek(0)
        file_content = await file.read(4096)
        file.file.seek(0)

        # 使用python-magic检测（如果可用）
        detected_mime = None
        if MAGIC_AVAILABLE and file_content:
            try:
                mime = magic.Magic(mime=True)
                detected_mime = mime.from_buffer(file_content)
            except Exception as magic_exc:
                logger.warning(
                    "python-magic检测失败",
                    extra={"error": str(magic_exc)}
                )
                detected_mime = None

        logger.debug(
            "文件类型检测",
            extra={
                "filename": filename,
                "extension": extension,
                "content_type": content_type,
                "detected_mime": detected_mime,
                "magic_available": MAGIC_AVAILABLE
            }
        )

        # 根据检测结果确定文件类型
        file_type = _determine_file_type(extension, content_type, detected_mime)

        if file_type == FileType.UNKNOWN:
            raise FileValidationError(
                "无法识别的文件类型",
                details={
                    "filename": filename,
                    "extension": extension,
                    "content_type": content_type,
                    "detected_mime": detected_mime
                }
            )

        return file_type

    except Exception as exc:
        logger.warning(
            "文件类型检测失败",
            extra={
                "filename": filename,
                "error": str(exc)
            }
        )
        raise FileValidationError(
            f"文件类型检测失败: {str(exc)}",
            details={"filename": filename}
        )


def _determine_file_type(
    extension: str,
    content_type: Optional[str],
    detected_mime: Optional[str]
) -> FileType:
    """根据扩展名、MIME类型和检测到的MIME类型确定文件类型"""
    # 扩展名映射
    extension_map = {
        "pdf": FileType.PDF,
        "docx": FileType.DOCX,
        "doc": FileType.DOC,
        "xlsx": FileType.XLSX,
        "xls": FileType.XLS,
        "pptx": FileType.PPTX,
        "ppt": FileType.PPT,
        "txt": FileType.TXT,
    }

    # MIME类型映射
    mime_map = {
        "application/pdf": FileType.PDF,
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document": FileType.DOCX,
        "application/msword": FileType.DOC,
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet": FileType.XLSX,
        "application/vnd.ms-excel": FileType.XLS,
        "application/vnd.openxmlformats-officedocument.presentationml.presentation": FileType.PPTX,
        "application/vnd.ms-powerpoint": FileType.PPT,
        "text/plain": FileType.TXT,
    }

    # 优先使用扩展名
    if extension in extension_map:
        return extension_map[extension]

    # 其次使用内容类型
    if content_type and content_type in mime_map:
        return mime_map[content_type]

    # 最后使用检测到的MIME类型
    if detected_mime and detected_mime in mime_map:
        return mime_map[detected_mime]

    return FileType.UNKNOWN


def is_file_type_supported(file_type: FileType) -> bool:
    """检查文件类型是否支持

    Args:
        file_type: 文件类型

    Returns:
        bool: 是否支持
    """
    return file_type.value in SUPPORTED_FORMATS


async def save_upload_file(file: UploadFile, temp_dir: Path) -> Path:
    """保存上传的文件到临时位置

    Args:
        file: 上传的文件
        temp_dir: 临时目录

    Returns:
        Path: 保存的文件路径
    """
    # 确保临时目录存在
    temp_dir.mkdir(parents=True, exist_ok=True)

    # 生成唯一的文件名
    import uuid
    filename = f"{uuid.uuid4()}_{file.filename}"
    file_path = temp_dir / filename

    # 异步保存文件
    try:
        # 使用aiofiles进行异步文件操作
        import aiofiles

        async with aiofiles.open(file_path, 'wb') as f:
            # 分块读取和写入
            chunk_size = 1024 * 1024  # 1MB
            while chunk := await file.read(chunk_size):
                await f.write(chunk)

        logger.debug(f"文件保存成功: {file_path}")
        return file_path

    except Exception as exc:
        # 如果保存失败，清理文件
        if file_path.exists():
            file_path.unlink(missing_ok=True)

        logger.error(
            "文件保存失败",
            extra={
                "filename": file.filename,
                "temp_dir": str(temp_dir),
                "error": str(exc)
            }
        )
        raise FileValidationError(
            f"文件保存失败: {str(exc)}",
            details={"filename": file.filename}
        )


def get_file_info(file_path: Path) -> Dict[str, Any]:
    """获取文件信息

    Args:
        file_path: 文件路径

    Returns:
        Dict[str, Any]: 文件信息
    """
    stat = file_path.stat()

    return {
        "path": str(file_path),
        "size": stat.st_size,
        "created": stat.st_ctime,
        "modified": stat.st_mtime,
        "name": file_path.name,
        "extension": file_path.suffix.lstrip('.')
    }
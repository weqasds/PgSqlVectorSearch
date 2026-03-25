"""验证工具函数"""
from typing import Dict, Any, Optional
import re

from rag_document_parser.models.schemas import ParseRequest, ParseStrategy
from rag_document_parser.services.exceptions import ValidationError
from rag_document_parser.utils.logging import get_logger

logger = get_logger(__name__)


def validate_parse_request(request: ParseRequest) -> None:
    """验证解析请求参数

    Args:
        request: 解析请求

    Raises:
        ValidationError: 验证失败
    """
    logger.debug("验证解析请求参数", extra={"request": request.model_dump()})

    # 验证解析策略
    _validate_strategy(request.strategy)

    # 验证OCR阈值
    if request.ocr_threshold is not None:
        _validate_ocr_threshold(request.ocr_threshold)

    # 验证超时时间
    if request.timeout is not None:
        _validate_timeout(request.timeout)

    # 验证组合逻辑
    _validate_request_combination(request)

    logger.debug("解析请求参数验证通过")


def _validate_strategy(strategy: ParseStrategy) -> None:
    """验证解析策略"""
    if not isinstance(strategy, ParseStrategy):
        raise ValidationError(
            "无效的解析策略",
            details={
                "strategy": strategy,
                "valid_strategies": [s.value for s in ParseStrategy]
            }
        )


def _validate_ocr_threshold(threshold: int) -> None:
    """验证OCR阈值"""
    if threshold < 0 or threshold > 100:
        raise ValidationError(
            "OCR阈值必须在0-100之间",
            details={"threshold": threshold}
        )


def _validate_timeout(timeout: int) -> None:
    """验证超时时间"""
    if timeout <= 0:
        raise ValidationError(
            "超时时间必须大于0",
            details={"timeout": timeout}
        )

    if timeout > 3600:  # 1小时
        logger.warning(f"超时时间较长: {timeout}秒")


def _validate_request_combination(request: ParseRequest) -> None:
    """验证请求参数组合"""
    # 检查策略与提取选项的一致性
    if request.strategy == ParseStrategy.TEXT_ONLY:
        if request.extract_tables or request.extract_images:
            logger.warning(
                "TEXT_ONLY策略下，表格和图片提取选项将被忽略",
                extra={
                    "strategy": request.strategy.value,
                    "extract_tables": request.extract_tables,
                    "extract_images": request.extract_images
                }
            )

    elif request.strategy == ParseStrategy.TABLES_ONLY:
        if not request.extract_tables:
            logger.warning(
                "TABLES_ONLY策略下，建议启用表格提取",
                extra={
                    "strategy": request.strategy.value,
                    "extract_tables": request.extract_tables
                }
            )

    # 检查OCR相关逻辑
    if request.strategy == ParseStrategy.OCR_ONLY:
        if request.ocr_threshold is None:
            logger.info("OCR_ONLY策略将处理所有页面")


def validate_file_extension(filename: str, allowed_extensions: list) -> bool:
    """验证文件扩展名

    Args:
        filename: 文件名
        allowed_extensions: 允许的扩展名列表（不含点）

    Returns:
        bool: 是否允许
    """
    import os
    _, ext = os.path.splitext(filename)
    ext = ext.lstrip('.').lower()

    return ext in allowed_extensions


def validate_mime_type(content_type: str, allowed_mime_types: list) -> bool:
    """验证MIME类型

    Args:
        content_type: 内容类型
        allowed_mime_types: 允许的MIME类型列表

    Returns:
        bool: 是否允许
    """
    return content_type in allowed_mime_types


def validate_file_path(file_path: str) -> None:
    """验证文件路径安全性

    Args:
        file_path: 文件路径

    Raises:
        ValidationError: 路径不安全
    """
    # 防止路径遍历攻击
    if ".." in file_path or "~" in file_path:
        raise ValidationError(
            "文件路径包含不安全字符",
            details={"file_path": file_path}
        )

    # 检查绝对路径
    import os
    if os.path.isabs(file_path):
        # 在生产环境中可能需要限制路径范围
        pass


def sanitize_filename(filename: str) -> str:
    """清理文件名，移除不安全字符

    Args:
        filename: 原始文件名

    Returns:
        str: 清理后的文件名
    """
    import os

    # 移除路径分隔符
    filename = filename.replace("/", "_").replace("\\", "_")

    # 移除控制字符
    filename = re.sub(r'[\x00-\x1f\x7f-\x9f]', '', filename)

    # 限制长度
    max_length = 255
    if len(filename) > max_length:
        name, ext = os.path.splitext(filename)
        filename = name[:max_length - len(ext)] + ext

    return filename


def validate_config_values(config: Dict[str, Any]) -> None:
    """验证配置值

    Args:
        config: 配置字典

    Raises:
        ValidationError: 配置验证失败
    """
    required_keys = ["PORT", "HOST", "MAX_UPLOAD_SIZE"]
    missing_keys = [key for key in required_keys if key not in config]

    if missing_keys:
        raise ValidationError(
            "缺少必需的配置项",
            details={"missing_keys": missing_keys}
        )

    # 验证数值范围
    if "PORT" in config:
        port = config["PORT"]
        if not isinstance(port, int) or port < 1 or port > 65535:
            raise ValidationError(
                "端口号必须在1-65535之间",
                details={"port": port}
            )

    if "MAX_UPLOAD_SIZE" in config:
        max_size = config["MAX_UPLOAD_SIZE"]
        if not isinstance(max_size, int) or max_size <= 0:
            raise ValidationError(
                "最大上传大小必须为正整数",
                details={"max_upload_size": max_size}
            )

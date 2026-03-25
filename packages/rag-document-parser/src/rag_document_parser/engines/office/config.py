"""Office引擎配置"""
from dataclasses import dataclass, field
from typing import Optional, Dict, Any


@dataclass
class OfficeEngineConfig:
    """Office引擎配置参数

    Attributes:
        max_file_size: 最大文件大小（字节），默认100MB
        extract_formulas: 是否提取Excel公式，默认False
        extract_comments: 是否提取批注，默认True
        extract_hyperlinks: 是否提取超链接，默认True
        image_extraction_mode: 图片提取模式，默认"metadata"
        preserve_formatting: 是否保留格式信息，默认True
    """
    # 文件大小限制
    max_file_size: int = 100 * 1024 * 1024  # 100MB
    # Excel特定配置
    extract_formulas: bool = False  # 提取Excel公式
    extract_comments: bool = True   # 提取批注
    # 通用配置
    extract_hyperlinks: bool = True  # 提取超链接
    image_extraction_mode: str = "metadata"  # "none", "metadata", "full"
    preserve_formatting: bool = True  # 是否保留格式信息
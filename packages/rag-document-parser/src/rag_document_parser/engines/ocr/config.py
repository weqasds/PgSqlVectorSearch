"""OCR引擎配置"""
from dataclasses import dataclass, field
from typing import List


@dataclass
class OCRConfig:
    """OCR配置参数

    Attributes:
        languages: 识别语言列表，默认中文简体+英文
        page_seg_mode: 页面分割模式 (0-13)，默认3（自动页面分割，无OSD）
        ocr_engine_mode: OCR引擎模式 (0-3)，默认3（基于LSTM的引擎）
        preprocess_image: 是否预处理图像（增强对比度、降噪等）
        timeout: OCR超时时间（秒）
        preserve_interword_spaces: 是否保留单词间空格
        output_type: 输出类型，默认"dict"
    """
    # 语言配置
    languages: List[str] = field(default_factory=lambda: ["chi_sim", "eng"])
    # 页面分割模式 (0-13)
    page_seg_mode: int = 3  # 默认：自动页面分割，无OSD
    # OCR引擎模式 (0-3)
    ocr_engine_mode: int = 3  # 默认：基于LSTM的引擎
    # 预处理配置
    preprocess_image: bool = True  # 是否预处理图像
    # 性能配置
    timeout: int = 30  # OCR超时时间（秒）
    # 输出配置
    preserve_interword_spaces: bool = True  # 保留单词间空格
    output_type: str = "dict"  # 输出类型：dict, string, hocr
"""OCR引擎"""
from .tesseract_engine import TesseractEngine
from .config import OCRConfig

__all__ = ["TesseractEngine", "OCRConfig"]
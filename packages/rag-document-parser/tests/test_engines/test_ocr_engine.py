"""OCR引擎测试"""
from unittest.mock import patch

import pytest
from PIL import Image

from rag_document_parser.engines.ocr.config import OCRConfig
from rag_document_parser.engines.ocr.tesseract_engine import TesseractEngine
from rag_document_parser.models.schemas import FileType, ParseRequest


def test_build_tesseract_config_reflects_custom_options():
    engine = TesseractEngine(config=OCRConfig(page_seg_mode=6, ocr_engine_mode=1, preserve_interword_spaces=False))

    config = engine._build_tesseract_config()

    assert "--psm 6" in config
    assert "--oem 1" in config
    assert "preserve_interword_spaces" not in config


@pytest.mark.asyncio
async def test_parse_image_returns_ocr_result(monkeypatch):
    engine = TesseractEngine(config=OCRConfig(preprocess_image=False))
    image = Image.new("RGB", (100, 40), color="white")
    ocr_payload = {
        "text": ["Hello", "OCR"],
        "block_num": [1, 1],
        "par_num": [1, 1],
        "line_num": [1, 1],
        "left": [1, 20],
        "top": [1, 1],
        "width": [10, 10],
        "height": [12, 12],
        "conf": [95, 95],
    }

    with patch("pytesseract.image_to_data", return_value=ocr_payload):
        result = await engine.parse_image(image, ParseRequest(), "req-ocr", file_type=FileType.PNG)

    assert result.ocr_triggered is True
    assert "Hello OCR" in result.content[0].text
    assert result.document_metadata.file_type == FileType.PNG


def test_preprocess_image_returns_image_instance():
    engine = TesseractEngine()
    image = Image.new("RGB", (20, 20), color="white")

    processed = engine._preprocess_image(image)

    assert isinstance(processed, Image.Image)

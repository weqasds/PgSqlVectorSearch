#!/usr/bin/env python3
"""简单的导入测试"""
import sys
import os

# 添加项目根目录到Python路径
project_root = os.path.dirname(os.path.abspath(__file__))
src_dir = os.path.join(project_root, "src")
if src_dir not in sys.path:
    sys.path.insert(0, src_dir)

# 测试模块列表
modules = [
    ("rag_document_parser", "主包"),
    ("rag_document_parser.api.app", "FastAPI应用"),
    ("rag_document_parser.services.config", "配置管理"),
    ("rag_document_parser.services.parser_service", "解析服务"),
    ("rag_document_parser.services.task_service", "任务服务"),
    ("rag_document_parser.models.schemas", "数据模型"),
    ("rag_document_parser.models.responses", "响应模型"),
    ("rag_document_parser.utils.logging", "日志工具"),
    ("rag_document_parser.utils.file_utils", "文件工具"),
    ("rag_document_parser.utils.validation", "验证工具"),
    ("rag_document_parser.engines.base", "引擎基类"),
    ("rag_document_parser.engines.pdf.pymupdf_engine", "PyMuPDF引擎"),
    ("rag_document_parser.engines.pdf.pdfplumber_engine", "PDFPlumber引擎"),
]

print("Testing imports...")
all_passed = True

for module_path, description in modules:
    try:
        __import__(module_path)
        print(f"[OK] {description}: import successful")
    except ImportError as e:
        print(f"[FAIL] {description}: import failed - {e}")
        all_passed = False
    except Exception as e:
        print(f"[WARN] {description}: import exception - {e}")
        all_passed = False

print(f"\nResult: {'All passed' if all_passed else 'Some failed'}")
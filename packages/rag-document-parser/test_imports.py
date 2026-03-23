#!/usr/bin/env python3
"""测试RAG Document Parser的导入和基本功能"""
import sys
import os
import io

# 设置UTF-8编码（Windows控制台兼容）
if sys.platform == "win32":
    # Windows控制台编码处理
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8', errors='replace')

# 添加项目根目录到Python路径
project_root = os.path.dirname(os.path.abspath(__file__))
src_dir = os.path.join(project_root, "src")
if src_dir not in sys.path:
    sys.path.insert(0, src_dir)

def test_imports():
    """测试所有主要模块的导入"""
    print("测试RAG Document Parser导入...")

    modules_to_test = [
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

    all_passed = True

    for module_path, description in modules_to_test:
        try:
            __import__(module_path)
            print(f"[OK] {description}: 导入成功")
        except ImportError as e:
            print(f"[FAIL] {description}: 导入失败 - {e}")
            all_passed = False
        except Exception as e:
            print(f"[WARN] {description}: 导入异常 - {e}")
            all_passed = False

    return all_passed

def test_config():
    """测试配置加载"""
    print("\n测试配置加载...")
    try:
        from rag_document_parser.services.config import settings
        print("[OK] 配置加载成功")
        print(f"   主机: {settings.HOST}")
        print(f"   端口: {settings.PORT}")
        print(f"   环境: {'开发' if settings.RELOAD else '生产'}")
        print(f"   日志级别: {settings.LOG_LEVEL}")
        return True
    except Exception as e:
        print(f"[FAIL] 配置加载失败: {e}")
        return False

def test_fastapi_app():
    """测试FastAPI应用创建"""
    print("\n测试FastAPI应用...")
    try:
        from rag_document_parser.api.app import app
        print(f"[OK] FastAPI应用创建成功")
        print(f"   标题: {app.title}")
        print(f"   版本: {app.version}")

        # 检查路由
        routes = [route.path for route in app.routes if hasattr(route, 'path')]
        print(f"   路由数量: {len(routes)}")
        print(f"   示例路由: {routes[:5] if routes else '无'}")

        return True
    except Exception as e:
        print(f"[FAIL] FastAPI应用创建失败: {e}")
        return False

def test_parser_service():
    """测试解析服务"""
    print("\n测试解析服务...")
    try:
        from rag_document_parser.services.parser_service import ParserService
        service = ParserService()
        print(f"[OK] 解析服务创建成功")
        print(f"   引擎数量: {len(service._engine_instances)}")
        return True
    except Exception as e:
        print(f"[FAIL] 解析服务创建失败: {e}")
        return False

def main():
    """主测试函数"""
    print("=" * 60)
    print("RAG Document Parser 导入测试")
    print("=" * 60)

    tests = [
        test_imports,
        test_config,
        test_fastapi_app,
        test_parser_service,
    ]

    results = []
    for test_func in tests:
        try:
            result = test_func()
            results.append(result)
        except Exception as e:
            print(f"[FAIL] 测试函数 {test_func.__name__} 异常: {e}")
            results.append(False)

    passed = sum(1 for r in results if r)
    total = len(results)

    print("\n" + "=" * 60)
    print(f"测试结果: {passed}/{total} 通过")
    print("=" * 60)

    if passed == total:
        print("[OK] 所有测试通过！")
        return 0
    else:
        print("[FAIL] 部分测试失败，请检查上述错误信息")
        return 1

if __name__ == "__main__":
    sys.exit(main())
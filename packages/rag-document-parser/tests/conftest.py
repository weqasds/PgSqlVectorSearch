"""测试配置"""
import pytest
from fastapi.testclient import TestClient

from rag_document_parser.api.app import app


@pytest.fixture
def client():
    """测试客户端"""
    with TestClient(app) as test_client:
        yield test_client


@pytest.fixture
def sample_pdf_file(tmp_path):
    """示例PDF文件"""
    pdf_path = tmp_path / "sample.pdf"
    # 创建一个简单的PDF文件（实际测试中需要真实文件）
    with open(pdf_path, "wb") as f:
        # 添加最小PDF内容
        f.write(b"%PDF-1.4\n1 0 obj\n<</Type/Catalog/Pages 2 0 R>>\nendobj\n2 0 obj\n<</Type/Pages/Kids[]/Count 0>>\nendobj\nxref\n0 3\n0000000000 65535 f \n0000000010 00000 n \n0000000053 00000 n \ntrailer\n<</Size 3/Root 1 0 R>>\nstartxref\n94\n%%EOF")
    return pdf_path
"""端到端集成测试"""
import fitz
import pytest


def test_validate_then_parse_pdf(client, sample_pdf_bytes):
    validate_response = client.post(
        "/parse/validate",
        files={"file": ("sample.pdf", sample_pdf_bytes, "application/pdf")},
    )
    assert validate_response.status_code == 200
    assert validate_response.json()["data"]["file_type"] == "pdf"

    parse_response = client.post(
        "/parse",
        files={"file": ("sample.pdf", sample_pdf_bytes, "application/pdf")},
        data={"strategy": "auto"},
    )
    assert parse_response.status_code == 200
    payload = parse_response.json()
    assert payload["success"] is True
    assert payload["data"]["document_metadata"]["file_type"] == "pdf"
    assert "Hello PDF parser" in payload["data"]["content"][0]["text"]


def test_async_parse_pdf_result_available(client, sample_pdf_bytes):
    create_response = client.post(
        "/parse/async",
        files={"file": ("async.pdf", sample_pdf_bytes, "application/pdf")},
        data={"strategy": "auto"},
    )
    assert create_response.status_code == 200
    task_id = create_response.json()["data"]["task_id"]

    result_response = client.get(f"/tasks/{task_id}/result")
    assert result_response.status_code == 200
    payload = result_response.json()
    assert payload["success"] is True
    assert payload["data"]["document_metadata"]["file_type"] == "pdf"


@pytest.mark.parametrize("path", ["/health", "/health/ready", "/ready", "/metrics"])
def test_operational_endpoints(client, path):
    response = client.get(path)
    assert response.status_code == 200

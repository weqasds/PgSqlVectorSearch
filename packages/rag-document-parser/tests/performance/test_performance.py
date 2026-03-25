"""轻量性能与监控测试"""
import time


def test_metrics_endpoint_exposes_custom_metrics(client):
    response = client.get("/metrics")

    assert response.status_code == 200
    metrics = response.text
    assert "document_parser_requests_total" in metrics
    assert "document_parser_request_duration_seconds" in metrics


def test_text_parse_endpoint_returns_quickly(client, sample_text_bytes):
    started = time.perf_counter()
    response = client.post(
        "/parse",
        files={"file": ("perf.txt", sample_text_bytes, "text/plain")},
        data={"strategy": "auto"},
    )
    elapsed = time.perf_counter() - started

    assert response.status_code == 200
    assert elapsed < 5

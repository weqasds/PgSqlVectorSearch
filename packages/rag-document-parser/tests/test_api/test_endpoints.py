"""API端点测试"""
import io

from rag_document_parser.services.config import settings


def test_parse_text_document_sync(client, sample_text_bytes):
    response = client.post(
        "/parse",
        files={"file": ("note.txt", sample_text_bytes, "text/plain")},
        data={"strategy": "auto"},
    )

    assert response.status_code == 200
    payload = response.json()
    assert payload["success"] is True
    assert payload["metadata"]["file_type"] == "txt"
    assert payload["data"]["document_metadata"]["file_type"] == "txt"
    assert "第一段" in payload["data"]["content"][0]["text"]


def test_async_parse_task_lifecycle(client, sample_text_bytes):
    response = client.post(
        "/parse/async",
        files={"file": ("async-note.txt", sample_text_bytes, "text/plain")},
        data={"strategy": "auto"},
    )

    assert response.status_code == 200
    payload = response.json()
    task_id = payload["data"]["task_id"]

    status_response = client.get(f"/tasks/{task_id}")
    assert status_response.status_code == 200
    status_payload = status_response.json()
    assert status_payload["data"]["status"] == "completed"

    result_response = client.get(f"/tasks/{task_id}/result")
    assert result_response.status_code == 200
    result_payload = result_response.json()
    assert result_payload["success"] is True
    assert result_payload["data"]["document_metadata"]["file_type"] == "txt"
    assert "第二段" in result_payload["data"]["content"][0]["text"]


def test_tasks_stats_route_and_legacy_async_route(client, sample_text_bytes):
    create_response = client.post(
        "/async/parse",
        files={"file": ("legacy.txt", sample_text_bytes, "text/plain")},
        data={"strategy": "auto"},
    )
    assert create_response.status_code == 200

    stats_response = client.get("/tasks/stats")
    assert stats_response.status_code == 200
    stats_payload = stats_response.json()
    assert stats_payload["success"] is True
    assert stats_payload["data"]["total"] >= 1


def test_ready_alias_available(client):
    response = client.get("/ready")

    assert response.status_code == 200
    payload = response.json()
    assert payload["success"] is True
    assert payload["data"]["status"] in {"healthy", "unhealthy"}


def test_protected_routes_enforce_auth_when_enabled(client, monkeypatch):
    monkeypatch.setattr(settings, "ENABLE_AUTH", True, raising=False)
    monkeypatch.setattr(settings, "AUTH_TOKENS", "secret-token", raising=False)

    unauthorized = client.get("/tasks/stats")
    assert unauthorized.status_code == 401

    authorized = client.get("/tasks/stats", headers={"Authorization": "Bearer secret-token"})
    assert authorized.status_code == 200

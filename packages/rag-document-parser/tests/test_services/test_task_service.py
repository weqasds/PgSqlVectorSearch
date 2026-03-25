"""任务服务测试"""
from pathlib import Path

from rag_document_parser.models.schemas import (
    DocumentMetadata,
    DocumentStructure,
    FileType,
    PageContent,
    PageMetadata,
    ParseRequest,
    ParseResult,
    TaskStatus,
)
from rag_document_parser.services.task_service import TaskService


def _sample_result() -> ParseResult:
    return ParseResult(
        content=[PageContent(page=1, text="done", tables=[], images=[], metadata=PageMetadata(page_number=1))],
        document_metadata=DocumentMetadata(
            title="doc",
            author=None,
            subject=None,
            keywords=[],
            created_date=None,
            modified_date=None,
            page_count=1,
            file_size=None,
            file_type=FileType.TXT,
        ),
        structure=DocumentStructure(),
        ocr_triggered=False,
    )


def test_create_and_complete_task(tmp_path):
    service = TaskService()
    file_path = tmp_path / "sample.txt"
    file_path.write_text("hello", encoding="utf-8")

    service.create_task(
        task_id="task-1",
        request_id="req-1",
        file_path=file_path,
        file_type=FileType.TXT,
        parse_request=ParseRequest(),
        temp_file_path=file_path,
        file_name=file_path.name,
    )
    service.update_task_status("task-1", TaskStatus.PROCESSING)
    service.complete_task("task-1", _sample_result(), 0.3)

    task = service.get_task("task-1")
    assert task["status"] == TaskStatus.COMPLETED
    assert task["result"]["document_metadata"]["file_type"] == "txt"
    assert task["progress"] == 1.0


def test_cancel_task_only_allows_pending_or_processing(tmp_path):
    service = TaskService()
    file_path = tmp_path / "sample.txt"
    file_path.write_text("hello", encoding="utf-8")

    service.create_task(
        task_id="task-2",
        request_id="req-2",
        file_path=file_path,
        file_type=FileType.TXT,
        parse_request=ParseRequest(),
    )
    assert service.cancel_task("task-2") is True
    assert service.cancel_task("task-2") is False


def test_cleanup_task_removes_temp_file_and_stats(tmp_path):
    service = TaskService()
    temp_file = tmp_path / "temp.txt"
    temp_file.write_text("hello", encoding="utf-8")

    service.create_task(
        task_id="task-3",
        request_id="req-3",
        file_path=temp_file,
        file_type=FileType.TXT,
        parse_request=ParseRequest(),
        temp_file_path=temp_file,
    )
    service.fail_task("task-3", "boom", processing_time=0.1)

    assert service.get_stats()["failed"] == 1
    assert service.cleanup_task("task-3") is True
    assert not temp_file.exists()
    assert service.get_stats()["total"] == 0

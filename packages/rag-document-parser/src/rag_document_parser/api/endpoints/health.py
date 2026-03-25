"""健康检查端点"""
import platform
import subprocess
import sys
from datetime import UTC, datetime
from typing import Any

from fastapi import APIRouter

from rag_document_parser.api.dependencies import RequestId
from rag_document_parser.models.responses import HealthResponse, create_success_response
from rag_document_parser.models.schemas import HealthCheckResponse
from rag_document_parser.services.config import settings

router = APIRouter()


def _check_python_dependencies() -> dict[str, Any]:
    dependencies: dict[str, Any] = {}

    try:
        import fitz

        dependencies["pymupdf"] = {"status": "healthy", "version": fitz.__doc__.split()[1]}
    except Exception as exc:
        dependencies["pymupdf"] = {"status": "unhealthy", "error": str(exc)}

    try:
        import pdfplumber

        dependencies["pdfplumber"] = {"status": "healthy", "version": pdfplumber.__version__}
    except Exception as exc:
        dependencies["pdfplumber"] = {"status": "unhealthy", "error": str(exc)}

    try:
        import pytesseract  # noqa: F401

        dependencies["pytesseract"] = {"status": "healthy"}
    except Exception as exc:
        dependencies["pytesseract"] = {"status": "unhealthy", "error": str(exc)}

    try:
        import docx  # noqa: F401
        import openpyxl  # noqa: F401
        import pptx  # noqa: F401

        dependencies["office_python"] = {"status": "healthy"}
    except Exception as exc:
        dependencies["office_python"] = {"status": "unhealthy", "error": str(exc)}

    return dependencies


def _check_binary(name: str, command: list[str]) -> dict[str, Any]:
    try:
        result = subprocess.run(command, capture_output=True, text=True, timeout=5, check=False)
    except Exception as exc:
        return {"status": "unhealthy", "error": str(exc)}

    if result.returncode != 0:
        return {
            "status": "unhealthy",
            "error": result.stderr.strip() or result.stdout.strip() or "执行失败",
        }

    version = (result.stdout or result.stderr).splitlines()[0].strip()
    return {"status": "healthy", "version": version or name}


def check_dependencies() -> dict[str, Any]:
    dependencies = _check_python_dependencies()

    tesseract_path = settings.get_tesseract_path()
    dependencies["tesseract"] = (
        _check_binary("tesseract", [str(tesseract_path), "--version"])
        if tesseract_path
        else {"status": "unhealthy", "error": "未找到"}
    )

    libreoffice_path = settings.get_libreoffice_path()
    dependencies["libreoffice"] = (
        _check_binary("libreoffice", [str(libreoffice_path), "--version"])
        if libreoffice_path
        else {"status": "unhealthy", "error": "未找到"}
    )

    return dependencies


def check_system_resources() -> dict[str, Any]:
    try:
        import psutil
    except ImportError:
        return {"error": "psutil未安装，无法检查系统资源"}

    memory = psutil.virtual_memory()
    disk = psutil.disk_usage(str(settings.TEMP_DIR))
    return {
        "cpu_percent": psutil.cpu_percent(interval=0.1),
        "memory": {
            "total_gb": memory.total / (1024**3),
            "available_gb": memory.available / (1024**3),
            "percent": memory.percent,
        },
        "disk": {
            "total_gb": disk.total / (1024**3),
            "free_gb": disk.free / (1024**3),
            "percent": disk.percent,
        },
    }


def _required_dependencies() -> list[str]:
    required = ["pymupdf", "pdfplumber", "office_python"]
    if settings.ENABLE_OCR:
        required.extend(["pytesseract", "tesseract"])
    if settings.ENABLE_OFFICE_CONVERSION:
        required.append("libreoffice")
    return required


@router.get("", response_model=HealthResponse)
async def health_check(request_id: RequestId) -> dict[str, Any]:
    start_time = datetime.now(UTC)
    dependencies = check_dependencies()
    resources = check_system_resources()

    unhealthy = [name for name, info in dependencies.items() if info.get("status") == "unhealthy"]
    status = "healthy" if not unhealthy else "degraded"

    if "memory" in resources and resources["memory"]["percent"] > 90:
        status = "degraded"
    if "disk" in resources and resources["disk"]["percent"] > 90:
        status = "degraded"

    response_data = HealthCheckResponse(
        status=status,
        version=__import__("rag_document_parser").__version__,
        timestamp=datetime.now(UTC),
        details={
            "service": "RAG Document Parser",
            "python_version": sys.version,
            "platform": platform.platform(),
            "dependencies": dependencies,
            "resources": resources,
            "enabled_features": {
                "ocr": settings.ENABLE_OCR,
                "office_conversion": settings.ENABLE_OFFICE_CONVERSION,
            },
        },
    )

    return create_success_response(
        data=response_data,
        processing_time=(datetime.now(UTC) - start_time).total_seconds(),
        metadata={"request_id": request_id},
    )


@router.get("/ready", response_model=HealthResponse)
async def readiness_check(request_id: RequestId) -> dict[str, Any]:
    start_time = datetime.now(UTC)
    settings.TEMP_DIR.mkdir(parents=True, exist_ok=True)

    dependencies = check_dependencies()
    required = _required_dependencies()
    missing_required = [name for name in required if dependencies.get(name, {}).get("status") != "healthy"]
    status = "healthy" if not missing_required else "unhealthy"

    response_data = HealthCheckResponse(
        status=status,
        version=__import__("rag_document_parser").__version__,
        timestamp=datetime.now(UTC),
        details={
            "service": "RAG Document Parser",
            "temp_dir": str(settings.TEMP_DIR),
            "required_dependencies": required,
            "missing_required": missing_required,
            "dependencies": dependencies,
        },
    )

    return create_success_response(
        data=response_data,
        processing_time=(datetime.now(UTC) - start_time).total_seconds(),
        metadata={"request_id": request_id},
    )


@router.get("/simple")
async def simple_health_check() -> dict[str, str]:
    return {"status": "healthy", "timestamp": datetime.now(UTC).isoformat()}



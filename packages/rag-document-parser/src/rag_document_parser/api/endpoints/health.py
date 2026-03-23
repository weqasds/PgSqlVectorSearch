"""健康检查端点"""
import platform
import sys
from datetime import datetime
from typing import Dict, Any

from fastapi import APIRouter, Depends
from fastapi.responses import JSONResponse

from rag_document_parser.api.dependencies import RequestId
from rag_document_parser.models.responses import HealthResponse, create_success_response
from rag_document_parser.models.schemas import HealthCheckResponse
from rag_document_parser.services.config import settings
from rag_document_parser.utils.logging import get_logger

logger = get_logger(__name__)
router = APIRouter()


def check_dependencies() -> Dict[str, Any]:
    """检查依赖"""
    dependencies = {}

    # 检查Python依赖
    try:
        import fitz  # PyMuPDF
        dependencies["pymupdf"] = {"status": "healthy", "version": fitz.__doc__.split()[1]}
    except ImportError:
        dependencies["pymupdf"] = {"status": "unhealthy", "error": "未安装"}

    try:
        import pdfplumber
        dependencies["pdfplumber"] = {"status": "healthy", "version": pdfplumber.__version__}
    except ImportError:
        dependencies["pdfplumber"] = {"status": "unhealthy", "error": "未安装"}

    try:
        import pytesseract
        dependencies["pytesseract"] = {"status": "healthy"}
    except ImportError:
        dependencies["pytesseract"] = {"status": "unhealthy", "error": "未安装"}

    # 检查系统依赖
    try:
        import subprocess
        # 检查Tesseract
        if settings.get_tesseract_path():
            result = subprocess.run(
                [str(settings.get_tesseract_path()), "--version"],
                capture_output=True,
                text=True,
                timeout=5,
            )
            if result.returncode == 0:
                dependencies["tesseract"] = {
                    "status": "healthy",
                    "version": result.stdout.split("\n")[0].strip(),
                }
            else:
                dependencies["tesseract"] = {"status": "unhealthy", "error": "无法执行"}
        else:
            dependencies["tesseract"] = {"status": "unhealthy", "error": "未找到"}
    except Exception as e:
        dependencies["tesseract"] = {"status": "unhealthy", "error": str(e)}

    return dependencies


def check_system_resources() -> Dict[str, Any]:
    """检查系统资源"""
    import psutil
    resources = {}

    try:
        # CPU使用率
        resources["cpu_percent"] = psutil.cpu_percent(interval=0.1)

        # 内存使用
        memory = psutil.virtual_memory()
        resources["memory"] = {
            "total_gb": memory.total / (1024**3),
            "available_gb": memory.available / (1024**3),
            "percent": memory.percent,
        }

        # 磁盘空间
        disk = psutil.disk_usage(settings.TEMP_DIR)
        resources["disk"] = {
            "total_gb": disk.total / (1024**3),
            "free_gb": disk.free / (1024**3),
            "percent": disk.percent,
        }

    except ImportError:
        resources["error"] = "psutil未安装，无法检查系统资源"

    return resources


@router.get("", response_model=HealthResponse)
async def health_check(request_id: RequestId) -> Dict[str, Any]:
    """健康检查端点"""
    start_time = datetime.utcnow()

    # 基本状态检查
    status = "healthy"
    details = {
        "service": "RAG Document Parser",
        "version": __import__("rag_document_parser").__version__,
        "python_version": sys.version,
        "platform": platform.platform(),
        "timestamp": datetime.utcnow().isoformat(),
    }

    # 检查依赖（如果有问题则标记为degraded）
    try:
        dependencies = check_dependencies()
        details["dependencies"] = dependencies

        # 检查是否有不健康的依赖
        unhealthy_deps = [
            name for name, info in dependencies.items()
            if info.get("status") == "unhealthy"
        ]
        if unhealthy_deps:
            status = "degraded"
            details["unhealthy_dependencies"] = unhealthy_deps

    except Exception as e:
        status = "degraded"
        details["dependency_check_error"] = str(e)

    # 检查系统资源
    try:
        resources = check_system_resources()
        details["resources"] = resources

        # 检查资源使用率
        if "memory" in resources and resources["memory"]["percent"] > 90:
            status = "degraded"
            details["high_memory_usage"] = True

        if "disk" in resources and resources["disk"]["percent"] > 90:
            status = "degraded"
            details["low_disk_space"] = True

    except Exception as e:
        # 资源检查失败不影响整体健康状态
        details["resource_check_error"] = str(e)

    processing_time = (datetime.utcnow() - start_time).total_seconds()

    response_data = HealthCheckResponse(
        status=status,
        version=__import__("rag_document_parser").__version__,
        timestamp=datetime.utcnow(),
        details=details,
    )

    return create_success_response(
        data=response_data.dict(),
        processing_time=processing_time,
        metadata={"request_id": request_id},
    )


@router.get("/ready", response_model=HealthResponse)
async def readiness_check(request_id: RequestId) -> Dict[str, Any]:
    """就绪检查端点"""
    start_time = datetime.utcnow()

    # 检查服务是否就绪（例如：数据库连接、外部服务等）
    status = "healthy"
    details = {
        "service": "RAG Document Parser",
        "status": "ready",
        "timestamp": datetime.utcnow().isoformat(),
    }

    # 检查临时目录
    temp_dir = settings.TEMP_DIR
    if not temp_dir.exists():
        try:
            temp_dir.mkdir(parents=True, exist_ok=True)
            details["temp_dir"] = {"status": "created", "path": str(temp_dir)}
        except Exception as e:
            status = "unhealthy"
            details["temp_dir"] = {"status": "error", "error": str(e)}

    # 检查依赖
    try:
        dependencies = check_dependencies()
        details["dependencies"] = dependencies

        # 必需依赖检查
        required_deps = ["pymupdf", "tesseract"]
        for dep in required_deps:
            if dep in dependencies and dependencies[dep].get("status") != "healthy":
                status = "unhealthy"
                details[f"missing_{dep}"] = True

    except Exception as e:
        status = "unhealthy"
        details["dependency_check_error"] = str(e)

    processing_time = (datetime.utcnow() - start_time).total_seconds()

    response_data = HealthCheckResponse(
        status=status,
        version=__import__("rag_document_parser").__version__,
        timestamp=datetime.utcnow(),
        details=details,
    )

    return create_success_response(
        data=response_data.dict(),
        processing_time=processing_time,
        metadata={"request_id": request_id},
    )


@router.get("/simple")
async def simple_health_check():
    """简单健康检查（用于负载均衡器）"""
    return {"status": "healthy", "timestamp": datetime.utcnow().isoformat()}
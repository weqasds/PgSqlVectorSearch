"""FastAPI应用主文件"""
from contextlib import asynccontextmanager
from pathlib import Path

from fastapi import FastAPI, Request
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse
from prometheus_fastapi_instrumentator import Instrumentator

from rag_document_parser.services.config import settings
from rag_document_parser.utils.logging import setup_logging

# 导入中间件
from .middleware.error_handler import ErrorHandlerMiddleware, SecurityHeadersMiddleware
from .middleware.logging import LoggingMiddleware, RequestIDMiddleware
from .middleware.metrics import MetricsMiddleware, RateLimitMiddleware

# 导入端点
from .endpoints import health, parse, async_, tasks

# 配置日志
logger = setup_logging()


@asynccontextmanager
async def lifespan(app: FastAPI):
    """应用生命周期管理"""
    # 启动时
    logger.info("启动 RAG Document Parser 服务")
    logger.info(f"版本: {__import__('rag_document_parser').__version__}")
    logger.info(f"环境: {'开发' if settings.RELOAD else '生产'}")
    logger.info(f"监听: {settings.HOST}:{settings.PORT}")

    # 创建必要目录
    Path(settings.TEMP_DIR).mkdir(parents=True, exist_ok=True)
    logger.info(f"临时目录: {settings.TEMP_DIR}")

    yield

    # 关闭时
    logger.info("关闭 RAG Document Parser 服务")


# 创建FastAPI应用
app = FastAPI(
    title="RAG Document Parser",
    description="文档解析服务，支持PDF、Office文档和OCR",
    version=__import__("rag_document_parser").__version__,
    lifespan=lifespan,
    docs_url="/docs" if not settings.TESTING else None,
    redoc_url="/redoc" if not settings.TESTING else None,
    openapi_url="/openapi.json" if not settings.TESTING else None,
)

# 添加CORS中间件
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # 生产环境应该限制来源
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# 添加自定义中间件
app.add_middleware(RequestIDMiddleware)
app.add_middleware(LoggingMiddleware)
app.add_middleware(ErrorHandlerMiddleware)
app.add_middleware(SecurityHeadersMiddleware)

if settings.ENABLE_METRICS:
    app.add_middleware(MetricsMiddleware)
    # 初始化Prometheus指标收集器
    instrumentator = Instrumentator(
        excluded_handlers=[settings.METRICS_PATH, "/health", "/health/ready"]
    )
    instrumentator.instrument(app).expose(app, endpoint=settings.METRICS_PATH)

if settings.ENABLE_RATE_LIMIT:
    app.add_middleware(RateLimitMiddleware, max_requests_per_second=settings.RATE_LIMIT_PER_SECOND)


# 全局异常处理
@app.exception_handler(Exception)
async def global_exception_handler(request: Request, exc: Exception):
    """全局异常处理"""
    logger.error(f"未处理的异常: {exc}", exc_info=exc)
    return JSONResponse(
        status_code=500,
        content={
            "success": False,
            "error": {
                "code": "INTERNAL_SERVER_ERROR",
                "message": "内部服务器错误",
                "details": str(exc) if settings.DEBUG else None,
            },
            "data": None,
            "metadata": {"request_id": getattr(request.state, "request_id", "")},
        },
    )


# 注册路由
app.include_router(health.router, prefix="/health", tags=["健康检查"])
app.include_router(parse.router, prefix="/parse", tags=["文档解析"])
app.include_router(async_.router, prefix="/parse/async", tags=["异步解析"])
app.include_router(tasks.router, prefix="/tasks", tags=["任务管理"])


@app.get("/")
async def root():
    """根端点"""
    return {
        "service": "RAG Document Parser",
        "version": __import__("rag_document_parser").__version__,
        "documentation": "/docs",
        "health": "/health",
        "ready": "/health/ready",
        "metrics": "/metrics" if settings.ENABLE_METRICS else None,
    }


@app.get("/info")
async def info():
    """服务信息"""
    return {
        "service": "RAG Document Parser",
        "version": __import__("rag_document_parser").__version__,
        "description": "文档解析服务，支持PDF、Office文档和OCR",
        "environment": "development" if settings.RELOAD else "production",
        "config": {
            "port": settings.PORT,
            "host": settings.HOST,
            "max_upload_size_mb": settings.max_upload_size_mb,
            "temp_dir": str(settings.TEMP_DIR),
            "pdf_engine": settings.PDF_ENGINE,
            "enable_ocr": settings.ENABLE_OCR,
            "enable_office_conversion": settings.ENABLE_OFFICE_CONVERSION,
        },
    }

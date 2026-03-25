"""指标与限流中间件"""
from collections import defaultdict, deque
import time
from typing import Callable

from fastapi import Request
from prometheus_client import Counter, Gauge, Histogram
from starlette.middleware.base import BaseHTTPMiddleware
from starlette.responses import JSONResponse, Response

from rag_document_parser.models.responses import create_error_response
from rag_document_parser.utils.logging import get_logger

logger = get_logger(__name__)

REQUESTS_TOTAL = Counter(
    "document_parser_requests_total",
    "文档解析服务请求总数",
    ["method", "endpoint", "status_code"],
)
REQUEST_DURATION = Histogram(
    "document_parser_request_duration_seconds",
    "文档解析服务请求耗时",
    ["method", "endpoint"],
)
PARSED_DOCUMENTS_TOTAL = Counter(
    "document_parser_parsed_documents_total",
    "成功完成的解析请求数",
    ["endpoint"],
)
ACTIVE_REQUESTS = Gauge(
    "document_parser_active_requests",
    "当前正在处理的请求数",
)


class MetricsMiddleware(BaseHTTPMiddleware):
    """指标收集中间件"""

    async def dispatch(self, request: Request, call_next: Callable) -> Response:
        start_time = time.time()
        ACTIVE_REQUESTS.inc()
        try:
            response = await call_next(request)
        finally:
            ACTIVE_REQUESTS.dec()

        process_time = time.time() - start_time
        endpoint = request.url.path
        REQUESTS_TOTAL.labels(request.method, endpoint, response.status_code).inc()
        REQUEST_DURATION.labels(request.method, endpoint).observe(process_time)

        if response.status_code < 400 and endpoint.startswith("/parse"):
            PARSED_DOCUMENTS_TOTAL.labels(endpoint=endpoint).inc()

        logger.debug(
            "请求指标",
            extra={
                "method": request.method,
                "endpoint": endpoint,
                "status_code": response.status_code,
                "process_time": process_time,
            },
        )
        return response


class RateLimitMiddleware(BaseHTTPMiddleware):
    """简单内存限流中间件"""

    def __init__(self, app, max_requests_per_second: int = 10):
        super().__init__(app)
        self.max_requests_per_second = max_requests_per_second
        self.request_records: dict[str, deque[float]] = defaultdict(deque)

    async def dispatch(self, request: Request, call_next: Callable) -> Response:
        client_ip = request.client.host if request.client else "unknown"
        current_time = time.time()
        records = self.request_records[client_ip]
        window_start = current_time - 1.0

        while records and records[0] < window_start:
            records.popleft()

        if len(records) >= self.max_requests_per_second:
            logger.warning(
                "速率限制触发",
                extra={"client_ip": client_ip, "url": str(request.url), "method": request.method},
            )
            error_response = create_error_response(
                code="RATE_LIMIT_EXCEEDED",
                message="请求过于频繁，请稍后再试",
                details={"limit": self.max_requests_per_second, "window": "1 second"},
                metadata={"request_id": getattr(request.state, "request_id", "")},
            )
            return JSONResponse(status_code=429, content=error_response, headers={"Retry-After": "1"})

        records.append(current_time)
        return await call_next(request)

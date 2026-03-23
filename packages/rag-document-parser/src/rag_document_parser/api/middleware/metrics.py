"""指标中间件"""
import time
from typing import Callable

from fastapi import Request
from starlette.middleware.base import BaseHTTPMiddleware
from starlette.responses import Response

from rag_document_parser.utils.logging import get_logger

logger = get_logger(__name__)


class MetricsMiddleware(BaseHTTPMiddleware):
    """指标收集中间件"""

    async def dispatch(self, request: Request, call_next: Callable) -> Response:
        # 记录开始时间
        start_time = time.time()

        # 处理请求
        response = await call_next(request)

        # 计算处理时间
        process_time = time.time() - start_time

        # 收集指标
        self._collect_metrics(request, response, process_time)

        return response

    def _collect_metrics(self, request: Request, response: Response, process_time: float):
        """收集指标数据"""
        # 这里收集的指标可以发送到Prometheus或其他监控系统
        metrics_data = {
            "method": request.method,
            "endpoint": request.url.path,
            "status_code": response.status_code,
            "process_time": process_time,
            "client_ip": request.client.host if request.client else "unknown",
            "user_agent": request.headers.get("user-agent", ""),
            "content_length": response.headers.get("content-length", 0),
        }

        # 记录到日志（实际应该发送到监控系统）
        logger.debug("请求指标", extra=metrics_data)

        # TODO: 发送到Prometheus
        # 这里可以集成prometheus_client


class RateLimitMiddleware(BaseHTTPMiddleware):
    """速率限制中间件"""

    def __init__(self, app, max_requests_per_second: int = 10):
        super().__init__(app)
        self.max_requests_per_second = max_requests_per_second
        self.request_counts = {}

    async def dispatch(self, request: Request, call_next: Callable) -> Response:
        client_ip = request.client.host if request.client else "unknown"
        current_time = time.time()

        # 清理旧记录
        self._cleanup_old_records(current_time)

        # 检查速率限制
        if self._is_rate_limited(client_ip, current_time):
            logger.warning(
                "速率限制触发",
                extra={
                    "client_ip": client_ip,
                    "url": str(request.url),
                    "method": request.method,
                },
            )

            from fastapi.responses import JSONResponse
            from rag_document_parser.models.responses import create_error_response

            error_response = create_error_response(
                code="RATE_LIMIT_EXCEEDED",
                message="请求过于频繁，请稍后再试",
                details={
                    "limit": self.max_requests_per_second,
                    "window": "1 second",
                },
            )

            return JSONResponse(
                status_code=429,
                content=error_response,
                headers={"Retry-After": "1"},
            )

        # 记录请求
        self._record_request(client_ip, current_time)

        # 继续处理
        return await call_next(request)

    def _cleanup_old_records(self, current_time: float):
        """清理1秒前的记录"""
        one_second_ago = current_time - 1.0
        self.request_counts = {
            ip: count for ip, count in self.request_counts.items()
            if count["timestamp"] > one_second_ago
        }

    def _is_rate_limited(self, client_ip: str, current_time: float) -> bool:
        """检查是否超过速率限制"""
        if client_ip not in self.request_counts:
            return False

        count_info = self.request_counts[client_ip]
        return count_info["count"] >= self.max_requests_per_second

    def _record_request(self, client_ip: str, current_time: float):
        """记录请求"""
        if client_ip not in self.request_counts:
            self.request_counts[client_ip] = {"count": 1, "timestamp": current_time}
        else:
            count_info = self.request_counts[client_ip]
            # 如果是在同一秒内，增加计数
            if current_time - count_info["timestamp"] < 1.0:
                count_info["count"] += 1
            else:
                # 否则重置计数
                count_info["count"] = 1
                count_info["timestamp"] = current_time
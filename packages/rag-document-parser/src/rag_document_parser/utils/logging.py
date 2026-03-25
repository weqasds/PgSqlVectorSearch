"""日志配置模块"""
import json
import logging
import sys
from pathlib import Path
from typing import Any, Callable

from loguru import logger

from rag_document_parser.services.config import settings


class InterceptHandler(logging.Handler):
    """拦截标准logging记录并重定向到loguru。"""

    def emit(self, record: logging.LogRecord) -> None:
        try:
            level: str | int = logger.level(record.levelname).name
        except ValueError:
            level = record.levelno

        frame, depth = logging.currentframe(), 2
        while frame and frame.f_code.co_filename == logging.__file__:
            frame = frame.f_back
            depth += 1

        logger.opt(depth=depth, exception=record.exc_info).log(level, record.getMessage())


def _serialize_log_value(value: Any) -> Any:
    if value is None or isinstance(value, (str, int, float, bool)):
        return value
    if isinstance(value, dict):
        return {str(key): _serialize_log_value(item) for key, item in value.items()}
    if isinstance(value, (list, tuple, set)):
        return [_serialize_log_value(item) for item in value]
    return str(value)


def json_log_format(record: dict[str, Any]) -> str:
    """JSON日志格式。"""
    log_entry = {
        "time": record["time"].isoformat(),
        "level": record["level"].name,
        "message": record["message"],
        "module": record["name"],
        "function": record["function"],
        "line": record["line"],
    }

    if record.get("extra"):
        log_entry["extra"] = _serialize_log_value(record["extra"])
    if record.get("exception"):
        log_entry["exception"] = str(record["exception"])

    rendered = json.dumps(log_entry, ensure_ascii=False)
    return rendered.replace("{", "{{").replace("}", "}}") + "\n"


def text_log_format(record: dict[str, Any]) -> str:
    """文本日志格式。"""
    time_value = record["time"].strftime("%Y-%m-%d %H:%M:%S.%f")[:-3]
    log_line = (
        f"{time_value} | {record['level'].name:<8} | "
        f"{record['name']}:{record['function']}:{record['line']} - {record['message']}"
    )
    if record.get("extra"):
        extra = " ".join(f"{key}={_serialize_log_value(value)}" for key, value in record["extra"].items())
        if extra:
            log_line += f" | {extra}"
    return log_line + "\n"


def setup_logging():
    """配置日志。"""
    logger.remove()

    formatter: Callable[[dict[str, Any]], str] = (
        json_log_format if settings.LOG_FORMAT.lower() == "json" else text_log_format
    )

    logger.add(
        sys.stderr,
        level=settings.LOG_LEVEL,
        format=formatter,
        colorize=settings.LOG_FORMAT.lower() == "text",
        backtrace=True,
        diagnose=settings.DEBUG,
    )

    if settings.LOG_FILE:
        log_path = Path(settings.LOG_FILE)
        log_path.parent.mkdir(parents=True, exist_ok=True)
        logger.add(
            str(log_path),
            level=settings.LOG_LEVEL,
            format=formatter,
            rotation="10 MB",
            retention="30 days",
            compression="zip",
            backtrace=True,
            diagnose=settings.DEBUG,
        )

    logging.basicConfig(handlers=[InterceptHandler()], level=0, force=True)
    logging.getLogger("uvicorn.access").disabled = True
    logging.getLogger("uvicorn.error").disabled = False
    return logger


def get_logger(name: str | None = None):
    """获取日志记录器。"""
    return logger.bind(module=name) if name else logger


log = get_logger(__name__)

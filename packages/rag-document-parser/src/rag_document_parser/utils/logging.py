"""日志配置模块"""
import json
import logging
import sys
from pathlib import Path
from typing import Any

from loguru import logger

from rag_document_parser.services.config import settings


class InterceptHandler(logging.Handler):
    """拦截标准logging记录并重定向到loguru"""

    def emit(self, record):
        # 获取对应的loguru级别
        try:
            level = logger.level(record.levelname).name
        except ValueError:
            level = record.levelno

        # 查找调用者
        frame, depth = logging.currentframe(), 2
        while frame.f_code.co_filename == logging.__file__:
            frame = frame.f_back
            depth += 1

        logger.opt(depth=depth, exception=record.exc_info).log(
            level, record.getMessage()
        )


def setup_logging():
    """配置日志"""
    # 移除默认处理器
    logger.remove()

    # 配置日志格式
    if settings.LOG_FORMAT.lower() == "json":
        format_str = json_log_format
    else:
        format_str = text_log_format

    # 控制台输出
    logger.add(
        sys.stderr,
        level=settings.LOG_LEVEL,
        serialize=True,
        colorize=True,
        backtrace=True,
        diagnose=settings.DEBUG,
    )

    # 文件输出（如果配置了日志文件）
    if settings.LOG_FILE:
        log_path = Path(settings.LOG_FILE)
        log_path.parent.mkdir(parents=True, exist_ok=True)

        logger.add(
            str(log_path),
            level=settings.LOG_LEVEL,
            serialize=True,
            rotation="10 MB",  # 每10MB轮转
            retention="30 days",  # 保留30天
            compression="zip",
            backtrace=True,
            diagnose=settings.DEBUG,
        )

    # 拦截标准logging
    logging.basicConfig(handlers=[InterceptHandler()], level=0, force=True)

    # 禁用第三方库的冗余日志
    logging.getLogger("uvicorn.access").disabled = True
    logging.getLogger("uvicorn.error").disabled = False

    return logger


def json_log_format(record: dict[str, Any]) -> str:
    """JSON日志格式"""
    log_entry = {
        "time": record["time"].isoformat()
        + "Z",
        "level": record["level"].name,
        "message": record["message"],
        "module": record["name"],
        "function": record["function"],
        "line": record["line"],
    }

    # 添加额外字段
    if record.get("extra"):
        log_entry.update(record["extra"])

    # 添加异常信息
    if record.get("exception"):
        log_entry["exception"] = str(record["exception"])

    return json.dumps(log_entry, ensure_ascii=False)


def text_log_format(record: dict[str, Any]) -> str:
    """文本日志格式"""
    time = record["time"].strftime(
        "%Y-%m-%d %H:%M:%S.%f"
    )[:-3]

    level = record["level"].name
    module = record["name"]
    function = record["function"]
    line = record["line"]
    message = record["message"]

    # 基本格式
    log_line = f"{time} | {level:<8} | {module}:{function}:{line} - {message}"

    # 添加额外字段
    if record.get("extra"):
        extra = " ".join([f"{k}={v}" for k, v in record["extra"].items()])
        log_line += f" | {extra}"

    return log_line


def get_logger(name: str = None):
    """获取日志记录器"""
    if name:
        return logger.bind(module=name)
    return logger


# 全局日志记录器
log = get_logger(__name__)
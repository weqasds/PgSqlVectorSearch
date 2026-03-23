"""配置管理模块"""
from pathlib import Path
from typing import Optional

from pydantic import Field, validator
from pydantic_settings import BaseSettings


class Settings(BaseSettings):
    """应用配置"""

    # 服务配置
    PORT: int = Field(default=8000, description="服务端口")
    HOST: str = Field(default="0.0.0.0", description="绑定主机")
    WORKERS: int = Field(default=1, description="工作进程数")
    RELOAD: bool = Field(default=True, description="热重载")

    # 文件处理配置
    MAX_UPLOAD_SIZE: int = Field(
        default=100 * 1024 * 1024, description="最大上传大小（字节）"
    )
    TEMP_DIR: Path = Field(default=Path("./temp"), description="临时文件目录")
    KEEP_TEMP_FILES: bool = Field(default=False, description="是否保留临时文件")

    # 解析配置
    PDF_ENGINE: str = Field(default="pymupdf", description="PDF引擎")
    ENABLE_OCR: bool = Field(default=True, description="启用OCR")
    OCR_THRESHOLD: int = Field(default=50, description="OCR触发阈值")
    ENABLE_OFFICE_CONVERSION: bool = Field(default=True, description="启用Office转换")
    LIBREOFFICE_PATH: Path | None = Field(default=None, description="LibreOffice路径")
    TESSERACT_PATH: Path | None = Field(default=None, description="Tesseract路径")

    # 性能配置
    MAX_CONCURRENT_TASKS: int = Field(default=5, description="最大并发任务数")
    TASK_TIMEOUT: int = Field(default=300, description="任务超时时间（秒）")
    OCR_TIMEOUT: int = Field(default=60, description="OCR超时时间（秒）")
    CONVERSION_TIMEOUT: int = Field(default=120, description="转换超时时间（秒）")

    # 日志配置
    LOG_LEVEL: str = Field(default="INFO", description="日志级别")
    LOG_FORMAT: str = Field(default="json", description="日志格式")
    LOG_FILE: Path | None = Field(default=None, description="日志文件路径")

    # 监控配置
    ENABLE_METRICS: bool = Field(default=True, description="启用Prometheus指标")
    METRICS_PATH: str = Field(default="/metrics", description="指标端点路径")
    ENABLE_HEALTH_CHECKS: bool = Field(default=True, description="启用健康检查")

    # 高级配置
    MEMORY_LIMIT_MB: int = Field(default=0, description="内存限制（MB，0为无限制）")
    CPU_LIMIT: float = Field(default=0.0, description="CPU限制（0.0-1.0）")
    ENABLE_RATE_LIMIT: bool = Field(default=False, description="启用请求限流")
    RATE_LIMIT_PER_SECOND: int = Field(default=10, description="每秒请求数限制")

    # 开发配置
    DEBUG: bool = Field(default=False, description="调试模式")
    LOG_SQL: bool = Field(default=False, description="记录SQL查询")
    TESTING: bool = Field(default=False, description="测试模式")

    class Config:
        """Pydantic配置"""
        env_file = ".env"
        env_file_encoding = "utf-8"
        case_sensitive = False

    @validator("TEMP_DIR", pre=True)
    def validate_temp_dir(cls, v):
        """验证临时目录"""
        if isinstance(v, str):
            path = Path(v)
            path.mkdir(parents=True, exist_ok=True)
            return path
        return v

    @validator("LOG_LEVEL")
    def validate_log_level(cls, v):
        """验证日志级别"""
        valid_levels = ["DEBUG", "INFO", "WARNING", "ERROR", "CRITICAL"]
        if v.upper() not in valid_levels:
            raise ValueError(f"LOG_LEVEL必须是以下之一: {', '.join(valid_levels)}")
        return v.upper()

    @validator("PDF_ENGINE")
    def validate_pdf_engine(cls, v):
        """验证PDF引擎"""
        valid_engines = ["pymupdf", "pdfplumber"]
        if v.lower() not in valid_engines:
            raise ValueError(f"PDF_ENGINE必须是以下之一: {', '.join(valid_engines)}")
        return v.lower()

    @validator("LOG_FORMAT")
    def validate_log_format(cls, v):
        """验证日志格式"""
        valid_formats = ["json", "text"]
        if v.lower() not in valid_formats:
            raise ValueError(f"LOG_FORMAT必须是以下之一: {', '.join(valid_formats)}")
        return v.lower()

    @property
    def temp_dir(self) -> Path:
        """获取临时目录"""
        return self.TEMP_DIR

    @property
    def max_upload_size_mb(self) -> float:
        """获取最大上传大小（MB）"""
        return self.MAX_UPLOAD_SIZE / (1024 * 1024)

    def get_libreoffice_path(self) -> Path | None:
        """获取LibreOffice路径"""
        if self.LIBREOFFICE_PATH:
            return self.LIBREOFFICE_PATH
        # 尝试自动检测（Windows）
        possible_paths = [
            Path("C:/Program Files/LibreOffice/program/soffice.exe"),
            Path("C:/Program Files (x86)/LibreOffice/program/soffice.exe"),
        ]
        for path in possible_paths:
            if path.exists():
                return path
        return None

    def get_tesseract_path(self) -> Path | None:
        """获取Tesseract路径"""
        if self.TESSERACT_PATH:
            return self.TESSERACT_PATH
        # 尝试自动检测（Windows）
        possible_paths = [
            Path("C:/Program Files/Tesseract-OCR/tesseract.exe"),
            Path("C:/Program Files (x86)/Tesseract-OCR/tesseract.exe"),
        ]
        for path in possible_paths:
            if path.exists():
                return path
        return None


# 全局配置实例
settings = Settings()


def print_config_summary():
    """打印配置摘要"""
    import json

    summary = {
        "服务配置": {
            "端口": settings.PORT,
            "主机": settings.HOST,
            "工作进程": settings.WORKERS,
            "热重载": settings.RELOAD,
        },
        "文件处理": {
            "最大上传大小(MB)": settings.max_upload_size_mb,
            "临时目录": str(settings.TEMP_DIR),
            "保留临时文件": settings.KEEP_TEMP_FILES,
        },
        "解析配置": {
            "PDF引擎": settings.PDF_ENGINE,
            "启用OCR": settings.ENABLE_OCR,
            "OCR阈值": settings.OCR_THRESHOLD,
            "启用Office转换": settings.ENABLE_OFFICE_CONVERSION,
        },
        "性能配置": {
            "最大并发任务": settings.MAX_CONCURRENT_TASKS,
            "任务超时(秒)": settings.TASK_TIMEOUT,
            "OCR超时(秒)": settings.OCR_TIMEOUT,
            "转换超时(秒)": settings.CONVERSION_TIMEOUT,
        },
    }

    print("RAG Document Parser 配置摘要:")
    print(json.dumps(summary, indent=2, ensure_ascii=False))


if __name__ == "__main__":
    print_config_summary()
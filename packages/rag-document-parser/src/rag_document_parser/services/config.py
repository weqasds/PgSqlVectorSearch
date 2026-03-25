"""配置管理模块"""

from pathlib import Path

from pydantic import Field, field_validator
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    """应用配置"""

    model_config = SettingsConfigDict(
        env_file=".env", env_file_encoding="utf-8", case_sensitive=False
    )

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
    PDF_ENGINE: str = Field(default="pymupdf", description="PDF主引擎")
    ENABLE_OCR: bool = Field(default=True, description="启用OCR")
    OCR_THRESHOLD: int = Field(default=50, description="OCR触发阈值（字符数）")
    ENABLE_OFFICE_CONVERSION: bool = Field(
        default=True, description="启用旧版Office转换"
    )
    LIBREOFFICE_PATH: Path | None = Field(default=None, description="LibreOffice路径")
    TESSERACT_PATH: Path | None = Field(default=None, description="Tesseract路径")

    # 性能配置
    MAX_CONCURRENT_TASKS: int = Field(default=5, description="最大并发任务数")
    TASK_TIMEOUT: int = Field(default=300, description="任务超时时间（秒）")
    OCR_TIMEOUT: int = Field(default=60, description="OCR超时时间（秒）")
    CONVERSION_TIMEOUT: int = Field(default=120, description="转换超时时间（秒）")

    # 安全与限流
    ENABLE_AUTH: bool = Field(default=False, description="是否启用Bearer鉴权")
    AUTH_TOKENS: str = Field(default="", description="逗号分隔的Bearer Token列表")
    ENABLE_RATE_LIMIT: bool = Field(default=False, description="启用请求限流")
    RATE_LIMIT_PER_SECOND: int = Field(default=10, description="每秒请求数限制")

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

    # 开发配置
    DEBUG: bool = Field(default=False, description="调试模式")
    LOG_SQL: bool = Field(default=False, description="记录SQL查询")
    TESTING: bool = Field(default=False, description="测试模式")

    @field_validator("TEMP_DIR", mode="before")
    @classmethod
    def validate_temp_dir(cls, value: str | Path) -> Path:
        """验证临时目录"""
        path = Path(value) if isinstance(value, str) else value
        path.mkdir(parents=True, exist_ok=True)
        return path

    @field_validator("LOG_LEVEL")
    @classmethod
    def validate_log_level(cls, value: str) -> str:
        """验证日志级别"""
        valid_levels = ["DEBUG", "INFO", "WARNING", "ERROR", "CRITICAL"]
        upper_value = value.upper()
        if upper_value not in valid_levels:
            raise ValueError(f"LOG_LEVEL必须是以下之一: {', '.join(valid_levels)}")
        return upper_value

    @field_validator("PDF_ENGINE")
    @classmethod
    def validate_pdf_engine(cls, value: str) -> str:
        """验证PDF引擎"""
        valid_engines = ["pymupdf", "pdfplumber"]
        lower_value = value.lower()
        if lower_value not in valid_engines:
            raise ValueError(f"PDF_ENGINE必须是以下之一: {', '.join(valid_engines)}")
        return lower_value

    @field_validator("LOG_FORMAT")
    @classmethod
    def validate_log_format(cls, value: str) -> str:
        """验证日志格式"""
        valid_formats = ["json", "text"]
        lower_value = value.lower()
        if lower_value not in valid_formats:
            raise ValueError(f"LOG_FORMAT必须是以下之一: {', '.join(valid_formats)}")
        return lower_value

    @property
    def temp_dir(self) -> Path:
        """获取临时目录"""
        return self.TEMP_DIR

    @property
    def max_upload_size_mb(self) -> float:
        """获取最大上传大小（MB）"""
        return self.MAX_UPLOAD_SIZE / (1024 * 1024)

    @property
    def auth_tokens(self) -> list[str]:
        """获取配置的鉴权Token列表"""
        return [token.strip() for token in self.AUTH_TOKENS.split(",") if token.strip()]

    def get_libreoffice_path(self) -> Path | None:
        """获取LibreOffice路径"""
        if self.LIBREOFFICE_PATH:
            return self.LIBREOFFICE_PATH

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

        possible_paths = [
            Path("C:/Program Files/Tesseract-OCR/tesseract.exe"),
            Path("C:/Program Files (x86)/Tesseract-OCR/tesseract.exe"),
        ]
        for path in possible_paths:
            if path.exists():
                return path
        return None


settings = Settings()


def print_config_summary() -> None:
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
        "安全配置": {
            "启用鉴权": settings.ENABLE_AUTH,
            "启用限流": settings.ENABLE_RATE_LIMIT,
            "每秒请求限制": settings.RATE_LIMIT_PER_SECOND,
        },
    }

    print("RAG Document Parser 配置摘要:")
    print(json.dumps(summary, indent=2, ensure_ascii=False))


if __name__ == "__main__":
    print_config_summary()

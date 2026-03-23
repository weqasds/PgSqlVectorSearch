"""自定义异常"""


class DocumentParserError(Exception):
    """文档解析基础异常"""

    def __init__(self, message: str, code: str = "DOCUMENT_PARSER_ERROR", details: dict = None):
        self.message = message
        self.code = code
        self.details = details or {}
        super().__init__(self.message)


class FileValidationError(DocumentParserError):
    """文件验证错误"""

    def __init__(self, message: str, details: dict = None):
        super().__init__(message, "FILE_VALIDATION_ERROR", details)


class UnsupportedFormatError(DocumentParserError):
    """不支持的格式错误"""

    def __init__(self, format: str, details: dict = None):
        message = f"不支持的文档格式: {format}"
        super().__init__(message, "UNSUPPORTED_FORMAT_ERROR", details or {"format": format})


class PDFParseError(DocumentParserError):
    """PDF解析错误"""

    def __init__(self, message: str, details: dict = None):
        super().__init__(message, "PDF_PARSE_ERROR", details)


class OCRProcessingError(DocumentParserError):
    """OCR处理错误"""

    def __init__(self, message: str, details: dict = None):
        super().__init__(message, "OCR_PROCESSING_ERROR", details)


class OfficeConversionError(DocumentParserError):
    """Office转换错误"""

    def __init__(self, message: str, details: dict = None):
        super().__init__(message, "OFFICE_CONVERSION_ERROR", details)


class ResourceLimitError(DocumentParserError):
    """资源限制错误"""

    def __init__(self, resource: str, limit: str, details: dict = None):
        message = f"{resource}超过限制: {limit}"
        super().__init__(message, "RESOURCE_LIMIT_ERROR", details or {"resource": resource, "limit": limit})


class TimeoutError(DocumentParserError):
    """超时错误"""

    def __init__(self, operation: str, timeout: int, details: dict = None):
        message = f"{operation}操作超时 ({timeout}秒)"
        super().__init__(message, "TIMEOUT_ERROR", details or {"operation": operation, "timeout": timeout})


class ConfigurationError(DocumentParserError):
    """配置错误"""

    def __init__(self, message: str, details: dict = None):
        super().__init__(message, "CONFIGURATION_ERROR", details)


class DependencyError(DocumentParserError):
    """依赖错误"""

    def __init__(self, dependency: str, message: str = None, details: dict = None):
        msg = message or f"缺少必要依赖: {dependency}"
        super().__init__(msg, "DEPENDENCY_ERROR", details or {"dependency": dependency})


class ValidationError(DocumentParserError):
    """验证错误"""

    def __init__(self, message: str, field: str = None, details: dict = None):
        super().__init__(message, "VALIDATION_ERROR", details or {"field": field})


class ParsingFailedError(DocumentParserError):
    """解析失败错误"""

    def __init__(self, message: str, engine: str = None, details: dict = None):
        super().__init__(message, "PARSING_FAILED_ERROR", details or {"engine": engine})


def handle_document_parser_error(exc: DocumentParserError) -> dict:
    """将DocumentParserError转换为API响应格式"""
    return {
        "success": False,
        "error": {
            "code": exc.code,
            "message": exc.message,
            "details": exc.details,
        },
        "data": None,
        "metadata": {},
    }
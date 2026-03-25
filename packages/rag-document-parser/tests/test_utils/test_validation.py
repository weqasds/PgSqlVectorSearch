"""验证工具测试"""
import pytest
from unittest.mock import Mock, patch
import os

from rag_document_parser.utils.validation import (
    validate_parse_request,
    _validate_strategy,
    _validate_ocr_threshold,
    _validate_timeout,
    _validate_request_combination,
    validate_file_extension,
    validate_mime_type,
    validate_file_path,
    sanitize_filename,
    validate_config_values,
)
from rag_document_parser.models.schemas import ParseRequest, ParseStrategy
from rag_document_parser.services.exceptions import ValidationError


class TestValidateParseRequest:
    """解析请求验证测试类"""

    def test_validate_parse_request_valid(self):
        """测试有效的解析请求"""
        request = ParseRequest(
            strategy=ParseStrategy.AUTO,
            extract_tables=True,
            extract_images=False,
            ocr_threshold=80,
            timeout=30
        )

        # 应该正常通过
        validate_parse_request(request)

    def test_validate_parse_request_invalid_strategy(self):
        """测试无效的解析策略"""
        # 创建一个无效策略的请求（需要模拟）
        request = Mock()
        request.strategy = "INVALID_STRATEGY"
        request.ocr_threshold = None
        request.timeout = None
        request.extract_tables = False
        request.extract_images = False
        request.dict = Mock(return_value={})

        # 应该抛出ValidationError
        with pytest.raises(ValidationError) as exc_info:
            validate_parse_request(request)

        # 验证异常
        assert "无效的解析策略" in str(exc_info.value)
        assert exc_info.value.details["strategy"] == "INVALID_STRATEGY"

    def test_validate_parse_request_invalid_ocr_threshold(self):
        """测试无效的OCR阈值"""
        request = ParseRequest(
            strategy=ParseStrategy.AUTO,
            extract_tables=True,
            extract_images=False,
            ocr_threshold=150,  # 无效：超过100
            timeout=30
        )

        # 应该抛出ValidationError
        with pytest.raises(ValidationError) as exc_info:
            validate_parse_request(request)

        # 验证异常
        assert "OCR阈值必须在0-100之间" in str(exc_info.value)
        assert exc_info.value.details["threshold"] == 150

    def test_validate_parse_request_negative_ocr_threshold(self):
        """测试负的OCR阈值"""
        request = ParseRequest(
            strategy=ParseStrategy.AUTO,
            extract_tables=True,
            extract_images=False,
            ocr_threshold=-10,  # 无效：负数
            timeout=30
        )

        # 应该抛出ValidationError
        with pytest.raises(ValidationError) as exc_info:
            validate_parse_request(request)

        assert "OCR阈值必须在0-100之间" in str(exc_info.value)

    def test_validate_parse_request_invalid_timeout(self):
        """测试无效的超时时间"""
        request = ParseRequest(
            strategy=ParseStrategy.AUTO,
            extract_tables=True,
            extract_images=False,
            ocr_threshold=None,
            timeout=0  # 无效：必须大于0
        )

        # 应该抛出ValidationError
        with pytest.raises(ValidationError) as exc_info:
            validate_parse_request(request)

        # 验证异常
        assert "超时时间必须大于0" in str(exc_info.value)
        assert exc_info.value.details["timeout"] == 0

    def test_validate_parse_request_negative_timeout(self):
        """测试负的超时时间"""
        request = ParseRequest(
            strategy=ParseStrategy.AUTO,
            extract_tables=True,
            extract_images=False,
            ocr_threshold=None,
            timeout=-10  # 无效：负数
        )

        # 应该抛出ValidationError
        with pytest.raises(ValidationError) as exc_info:
            validate_parse_request(request)

        assert "超时时间必须大于0" in str(exc_info.value)

    def test_validate_parse_request_large_timeout(self):
        """测试较大的超时时间（应该警告但通过）"""
        request = ParseRequest(
            strategy=ParseStrategy.AUTO,
            extract_tables=True,
            extract_images=False,
            ocr_threshold=None,
            timeout=7200  # 2小时，超过1小时
        )

        # 应该正常通过（但有警告日志）
        with patch('rag_document_parser.utils.validation.logger') as mock_logger:
            validate_parse_request(request)
            # 验证记录了警告
            mock_logger.warning.assert_called_once()


class TestValidateStrategy:
    """解析策略验证测试类"""

    def test_validate_strategy_valid(self):
        """测试有效的解析策略"""
        # 所有枚举值都应该有效
        for strategy in ParseStrategy:
            _validate_strategy(strategy)
        # 应该正常通过

    def test_validate_strategy_invalid(self):
        """测试无效的解析策略"""
        # 无效的策略值
        invalid_strategy = "INVALID_STRATEGY"

        # 应该抛出ValidationError
        with pytest.raises(ValidationError) as exc_info:
            _validate_strategy(invalid_strategy)

        # 验证异常
        assert "无效的解析策略" in str(exc_info.value)
        assert exc_info.value.details["strategy"] == invalid_strategy
        assert "valid_strategies" in exc_info.value.details

    def test_validate_strategy_none(self):
        """测试None策略"""
        # 应该抛出ValidationError
        with pytest.raises(ValidationError) as exc_info:
            _validate_strategy(None)

        assert "无效的解析策略" in str(exc_info.value)


class TestValidateOCRThreshold:
    """OCR阈值验证测试类"""

    def test_validate_ocr_threshold_valid(self):
        """测试有效的OCR阈值"""
        # 边界值
        _validate_ocr_threshold(0)   # 最小值
        _validate_ocr_threshold(50)  # 中间值
        _validate_ocr_threshold(100) # 最大值
        # 应该正常通过

    def test_validate_ocr_threshold_below_min(self):
        """测试低于最小值的OCR阈值"""
        with pytest.raises(ValidationError) as exc_info:
            _validate_ocr_threshold(-1)

        assert "OCR阈值必须在0-100之间" in str(exc_info.value)
        assert exc_info.value.details["threshold"] == -1

    def test_validate_ocr_threshold_above_max(self):
        """测试高于最大值的OCR阈值"""
        with pytest.raises(ValidationError) as exc_info:
            _validate_ocr_threshold(101)

        assert "OCR阈值必须在0-100之间" in str(exc_info.value)
        assert exc_info.value.details["threshold"] == 101


class TestValidateTimeout:
    """超时时间验证测试类"""

    def test_validate_timeout_valid(self):
        """测试有效的超时时间"""
        _validate_timeout(1)      # 最小值
        _validate_timeout(30)     # 典型值
        _validate_timeout(3600)   # 1小时
        # 应该正常通过

    def test_validate_timeout_zero(self):
        """测试零超时时间"""
        with pytest.raises(ValidationError) as exc_info:
            _validate_timeout(0)

        assert "超时时间必须大于0" in str(exc_info.value)
        assert exc_info.value.details["timeout"] == 0

    def test_validate_timeout_negative(self):
        """测试负超时时间"""
        with pytest.raises(ValidationError) as exc_info:
            _validate_timeout(-10)

        assert "超时时间必须大于0" in str(exc_info.value)

    def test_validate_timeout_large_warning(self):
        """测试大超时时间（应记录警告）"""
        with patch('rag_document_parser.utils.validation.logger') as mock_logger:
            _validate_timeout(7200)  # 2小时，超过1小时
            # 验证记录了警告
            mock_logger.warning.assert_called_once()


class TestValidateRequestCombination:
    """请求参数组合验证测试类"""

    def test_text_only_strategy_with_extraction(self):
        """测试TEXT_ONLY策略下的提取选项"""
        request = ParseRequest(
            strategy=ParseStrategy.TEXT_ONLY,
            extract_tables=True,   # 应该被忽略
            extract_images=True,   # 应该被忽略
            ocr_threshold=None,
            timeout=30
        )

        with patch('rag_document_parser.utils.validation.logger') as mock_logger:
            _validate_request_combination(request)
            # 验证记录了警告
            mock_logger.warning.assert_called_once()

    def test_tables_only_strategy_without_tables(self):
        """测试TABLES_ONLY策略下未启用表格提取"""
        request = ParseRequest(
            strategy=ParseStrategy.TABLES_ONLY,
            extract_tables=False,  # 建议启用
            extract_images=False,
            ocr_threshold=None,
            timeout=30
        )

        with patch('rag_document_parser.utils.validation.logger') as mock_logger:
            _validate_request_combination(request)
            # 验证记录了警告
            mock_logger.warning.assert_called_once()

    def test_ocr_only_strategy_without_threshold(self):
        """测试OCR_ONLY策略下没有阈值"""
        request = ParseRequest(
            strategy=ParseStrategy.OCR_ONLY,
            extract_tables=False,
            extract_images=False,
            ocr_threshold=None,  # 可以接受
            timeout=30
        )

        with patch('rag_document_parser.utils.validation.logger') as mock_logger:
            _validate_request_combination(request)
            # 验证记录了信息
            mock_logger.info.assert_called_once()

    def test_auto_strategy_with_all_options(self):
        """测试AUTO策略下的所有选项"""
        request = ParseRequest(
            strategy=ParseStrategy.AUTO,
            extract_tables=True,
            extract_images=True,
            ocr_threshold=80,
            timeout=30
        )

        # 应该正常通过，没有警告
        with patch('rag_document_parser.utils.validation.logger') as mock_logger:
            _validate_request_combination(request)
            # 不应该有警告
            assert not mock_logger.warning.called
            assert not mock_logger.info.called


class TestValidateFileExtension:
    """文件扩展名验证测试类"""

    def test_validate_file_extension_valid(self):
        """测试有效的文件扩展名"""
        assert validate_file_extension("document.pdf", ["pdf", "txt", "docx"]) is True
        assert validate_file_extension("image.png", ["png", "jpg", "jpeg"]) is True
        assert validate_file_extension("data.txt", ["txt", "csv", "json"]) is True

    def test_validate_file_extension_invalid(self):
        """测试无效的文件扩展名"""
        assert validate_file_extension("document.pdf", ["txt", "docx", "xlsx"]) is False
        assert validate_file_extension("image.png", ["pdf", "docx", "xlsx"]) is False

    def test_validate_file_extension_case_insensitive(self):
        """测试扩展名大小写不敏感"""
        assert validate_file_extension("document.PDF", ["pdf", "txt"]) is True  # 大写扩展名
        assert validate_file_extension("document.Pdf", ["PDF", "TXT"]) is False  # 列表中的大写

    def test_validate_file_extension_no_extension(self):
        """测试没有扩展名的文件"""
        assert validate_file_extension("document", ["pdf", "txt"]) is False
        assert validate_file_extension("", ["pdf", "txt"]) is False

    def test_validate_file_extension_multiple_dots(self):
        """测试多个点的文件名"""
        assert validate_file_extension("archive.tar.gz", ["gz", "zip"]) is True
        assert validate_file_extension("backup.2024.01.01.tar", ["tar", "zip"]) is True

    def test_validate_file_extension_empty_list(self):
        """测试空扩展名列表"""
        assert validate_file_extension("document.pdf", []) is False


class TestValidateMimeType:
    """MIME类型验证测试类"""

    def test_validate_mime_type_valid(self):
        """测试有效的MIME类型"""
        assert validate_mime_type("application/pdf", ["application/pdf", "text/plain"]) is True
        assert validate_mime_type("text/plain", ["application/pdf", "text/plain"]) is True

    def test_validate_mime_type_invalid(self):
        """测试无效的MIME类型"""
        assert validate_mime_type("application/pdf", ["text/plain", "image/png"]) is False
        assert validate_mime_type("image/jpeg", ["application/pdf", "text/plain"]) is False

    def test_validate_mime_type_case_sensitive(self):
        """测试MIME类型大小写敏感"""
        # MIME类型通常是大小写敏感的
        assert validate_mime_type("Application/PDF", ["application/pdf"]) is False
        assert validate_mime_type("application/PDF", ["application/pdf"]) is False

    def test_validate_mime_type_empty(self):
        """测试空MIME类型"""
        assert validate_mime_type("", ["application/pdf", "text/plain"]) is False
        assert validate_mime_type(None, ["application/pdf", "text/plain"]) is False

    def test_validate_mime_type_empty_list(self):
        """测试空MIME类型列表"""
        assert validate_mime_type("application/pdf", []) is False


class TestValidateFilePath:
    """文件路径安全性验证测试类"""

    def test_validate_file_path_safe(self):
        """测试安全的文件路径"""
        validate_file_path("documents/report.pdf")
        validate_file_path("data/files/document_2024.pdf")
        validate_file_path("test.txt")
        # 应该正常通过

    def test_validate_file_path_with_path_traversal(self):
        """测试包含路径遍历的文件路径"""
        # 应该抛出ValidationError
        with pytest.raises(ValidationError) as exc_info:
            validate_file_path("../../etc/passwd")

        assert "文件路径包含不安全字符" in str(exc_info.value)
        assert exc_info.value.details["file_path"] == "../../etc/passwd"

    def test_validate_file_path_with_tilde(self):
        """测试包含波浪号的文件路径"""
        # 应该抛出ValidationError
        with pytest.raises(ValidationError) as exc_info:
            validate_file_path("~/secret.txt")

        assert "文件路径包含不安全字符" in str(exc_info.value)

    def test_validate_file_path_absolute(self):
        """测试绝对路径"""
        # 绝对路径应该通过（但可能有其他限制）
        abs_path = os.path.abspath("test.txt")
        validate_file_path(abs_path)  # 应该正常通过

    def test_validate_file_path_mixed_unsafe(self):
        """测试混合不安全字符"""
        with pytest.raises(ValidationError):
            validate_file_path("documents/../..//etc/passwd")

        with pytest.raises(ValidationError):
            validate_file_path("~/../etc/passwd")


class TestSanitizeFilename:
    """文件名清理测试类"""

    def test_sanitize_filename_safe(self):
        """测试安全的文件名"""
        original = "document.pdf"
        sanitized = sanitize_filename(original)
        assert sanitized == "document.pdf"

    def test_sanitize_filename_with_slashes(self):
        """测试包含斜杠的文件名"""
        original = "path/to/document.pdf"
        sanitized = sanitize_filename(original)
        assert sanitized == "path_to_document.pdf"

    def test_sanitize_filename_with_backslashes(self):
        """测试包含反斜杠的文件名"""
        original = "path\\to\\document.pdf"
        sanitized = sanitize_filename(original)
        assert sanitized == "path_to_document.pdf"

    def test_sanitize_filename_with_control_chars(self):
        """测试包含控制字符的文件名"""
        original = "document\x00\x1f\x7f.pdf"
        sanitized = sanitize_filename(original)
        assert sanitized == "document.pdf"

    def test_sanitize_filename_too_long(self):
        """测试过长的文件名"""
        # 创建长文件名
        long_name = "a" * 300 + ".pdf"
        sanitized = sanitize_filename(long_name)

        # 验证长度不超过255
        assert len(sanitized) <= 255
        assert sanitized.endswith(".pdf")

    def test_sanitize_filename_very_long_extension(self):
        """测试扩展名很长的文件名"""
        long_ext = "a" * 300
        filename = f"document.{long_ext}"
        sanitized = sanitize_filename(filename)

        # 扩展名应该保留
        assert sanitized.endswith(f".{long_ext}")
        # 但总长度可能超过255，因为扩展名很长

    def test_sanitize_filename_empty(self):
        """测试空文件名"""
        sanitized = sanitize_filename("")
        assert sanitized == ""

    def test_sanitize_filename_only_extension(self):
        """测试只有扩展名的文件名"""
        sanitized = sanitize_filename(".pdf")
        assert sanitized == ".pdf"


class TestValidateConfigValues:
    """配置值验证测试类"""

    def test_validate_config_values_valid(self):
        """测试有效的配置"""
        config = {
            "PORT": 8000,
            "HOST": "localhost",
            "MAX_UPLOAD_SIZE": 10 * 1024 * 1024,
            "OTHER_SETTING": "value"
        }

        validate_config_values(config)
        # 应该正常通过

    def test_validate_config_values_missing_keys(self):
        """测试缺少必需键的配置"""
        config = {
            "PORT": 8000,
            # 缺少HOST
            "MAX_UPLOAD_SIZE": 10 * 1024 * 1024
        }

        with pytest.raises(ValidationError) as exc_info:
            validate_config_values(config)

        assert "缺少必需的配置项" in str(exc_info.value)
        assert "missing_keys" in exc_info.value.details
        assert "HOST" in exc_info.value.details["missing_keys"]

    def test_validate_config_values_invalid_port(self):
        """测试无效的端口号"""
        config = {
            "PORT": 0,  # 无效：小于1
            "HOST": "localhost",
            "MAX_UPLOAD_SIZE": 10 * 1024 * 1024
        }

        with pytest.raises(ValidationError) as exc_info:
            validate_config_values(config)

        assert "端口号必须在1-65535之间" in str(exc_info.value)
        assert exc_info.value.details["port"] == 0

    def test_validate_config_values_port_too_high(self):
        """测试过高的端口号"""
        config = {
            "PORT": 70000,  # 无效：超过65535
            "HOST": "localhost",
            "MAX_UPLOAD_SIZE": 10 * 1024 * 1024
        }

        with pytest.raises(ValidationError) as exc_info:
            validate_config_values(config)

        assert "端口号必须在1-65535之间" in str(exc_info.value)

    def test_validate_config_values_invalid_port_type(self):
        """测试无效的端口类型"""
        config = {
            "PORT": "8000",  # 无效：字符串而不是整数
            "HOST": "localhost",
            "MAX_UPLOAD_SIZE": 10 * 1024 * 1024
        }

        with pytest.raises(ValidationError) as exc_info:
            validate_config_values(config)

        assert "端口号必须在1-65535之间" in str(exc_info.value)

    def test_validate_config_values_invalid_max_size(self):
        """测试无效的最大上传大小"""
        config = {
            "PORT": 8000,
            "HOST": "localhost",
            "MAX_UPLOAD_SIZE": 0  # 无效：必须大于0
        }

        with pytest.raises(ValidationError) as exc_info:
            validate_config_values(config)

        assert "最大上传大小必须为正整数" in str(exc_info.value)
        assert exc_info.value.details["max_upload_size"] == 0

    def test_validate_config_values_negative_max_size(self):
        """测试负的最大上传大小"""
        config = {
            "PORT": 8000,
            "HOST": "localhost",
            "MAX_UPLOAD_SIZE": -1  # 无效：负数
        }

        with pytest.raises(ValidationError) as exc_info:
            validate_config_values(config)

        assert "最大上传大小必须为正整数" in str(exc_info.value)

    def test_validate_config_values_invalid_max_size_type(self):
        """测试无效的最大上传大小类型"""
        config = {
            "PORT": 8000,
            "HOST": "localhost",
            "MAX_UPLOAD_SIZE": "10MB"  # 无效：字符串而不是整数
        }

        with pytest.raises(ValidationError) as exc_info:
            validate_config_values(config)

        assert "最大上传大小必须为正整数" in str(exc_info.value)
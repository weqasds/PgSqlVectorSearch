"""Pydantic数据模型"""
from datetime import datetime
from enum import Enum
from typing import Any

from pydantic import BaseModel, Field


class FileType(str, Enum):
    """文件类型枚举"""

    PDF = "pdf"
    DOC = "doc"
    DOCX = "docx"
    XLS = "xls"
    XLSX = "xlsx"
    PPT = "ppt"
    PPTX = "pptx"
    TXT = "txt"
    PNG = "png"
    JPEG = "jpeg"
    JPG = "jpg"
    TIFF = "tiff"
    TIF = "tif"
    BMP = "bmp"
    GIF = "gif"
    WEBP = "webp"
    PPM = "ppm"
    PGM = "pgm"
    PBM = "pbm"
    PNM = "pnm"
    UNKNOWN = "unknown"


class ParseStrategy(str, Enum):
    """解析策略枚举"""

    TEXT_ONLY = "text_only"
    TABLES_ONLY = "tables_only"
    TEXT_AND_TABLES = "text_and_tables"
    OCR_ONLY = "ocr_only"
    AUTO = "auto"


class TaskStatus(str, Enum):
    """任务状态枚举"""

    PENDING = "pending"
    PROCESSING = "processing"
    COMPLETED = "completed"
    FAILED = "failed"
    CANCELLED = "cancelled"


class BoundingBox(BaseModel):
    """边界框"""

    x1: float = Field(..., description="左上角x坐标")
    y1: float = Field(..., description="左上角y坐标")
    x2: float = Field(..., description="右下角x坐标")
    y2: float = Field(..., description="右下角y坐标")

    @property
    def width(self) -> float:
        return self.x2 - self.x1

    @property
    def height(self) -> float:
        return self.y2 - self.y1


class TableCell(BaseModel):
    """表格单元格"""

    row: int = Field(..., description="行索引")
    col: int = Field(..., description="列索引")
    text: str = Field(..., description="单元格文本")
    bbox: BoundingBox | None = Field(None, description="边界框")


class Table(BaseModel):
    """表格"""

    table_index: int = Field(..., description="表格索引")
    rows: list[list[str]] = Field(..., description="表格数据")
    bbox: BoundingBox | None = Field(None, description="边界框")
    cells: list[TableCell] | None = Field(None, description="单元格详细信息")


class ImageInfo(BaseModel):
    """图片信息"""

    image_index: int = Field(..., description="图片索引")
    description: str | None = Field(None, description="图片描述（OCR结果）")
    bbox: BoundingBox | None = Field(None, description="边界框")


class PageMetadata(BaseModel):
    """页面元数据"""

    dimensions: dict[str, float] = Field(
        default_factory=lambda: {"width": 0.0, "height": 0.0}
    )
    rotation: int = Field(default=0, description="旋转角度")
    page_number: int = Field(default=1, description="页码")


class PageContent(BaseModel):
    """页面内容"""

    page: int = Field(..., description="页码（从1开始）")
    text: str = Field(default="", description="页面文本内容")
    tables: list[Table] = Field(default_factory=list, description="表格数据")
    images: list[ImageInfo] = Field(default_factory=list, description="图片信息")
    metadata: PageMetadata = Field(
        default_factory=lambda: PageMetadata(page_number=1), description="页面元数据"
    )


class DocumentMetadata(BaseModel):
    """文档元数据"""

    title: str | None = Field(None, description="文档标题")
    author: str | None = Field(None, description="作者")
    subject: str | None = Field(None, description="主题")
    keywords: list[str] = Field(default_factory=list, description="关键词")
    created_date: datetime | None = Field(None, description="创建日期")
    modified_date: datetime | None = Field(None, description="修改日期")
    page_count: int = Field(..., description="总页数")
    file_size: int | None = Field(None, description="文件大小（字节）")
    file_type: FileType = Field(..., description="文件类型")


class Heading(BaseModel):
    """标题"""

    level: int = Field(..., ge=1, le=6, description="标题级别（1-6）")
    text: str = Field(..., description="标题文本")
    page: int = Field(..., description="所在页码")
    bbox: BoundingBox | None = Field(None, description="边界框")


class Paragraph(BaseModel):
    """段落"""

    text: str = Field(..., description="段落文本")
    page: int = Field(..., description="所在页码")
    bbox: BoundingBox | None = Field(None, description="边界框")


class DocumentStructure(BaseModel):
    """文档结构"""

    headings: list[Heading] = Field(default_factory=list, description="标题层级")
    paragraphs: list[Paragraph] = Field(default_factory=list, description="段落信息")


class ParseRequest(BaseModel):
    """解析请求"""

    strategy: ParseStrategy = Field(default=ParseStrategy.AUTO, description="解析策略")
    extract_tables: bool = Field(default=True, description="是否提取表格")
    extract_images: bool = Field(default=False, description="是否提取图片信息")
    ocr_threshold: int | None = Field(None, description="OCR触发阈值")
    timeout: int | None = Field(None, description="超时时间（秒）")


class ParseResult(BaseModel):
    """解析结果"""

    content: list[PageContent] = Field(..., description="文档内容")
    document_metadata: DocumentMetadata = Field(..., description="文档级元数据")
    structure: DocumentStructure = Field(
        default_factory=DocumentStructure, description="文档结构信息"
    )
    ocr_triggered: bool = Field(default=False, description="是否触发了OCR")


class TaskInfo(BaseModel):
    """任务信息"""

    task_id: str = Field(..., description="任务ID")
    status: TaskStatus = Field(..., description="任务状态")
    created_at: datetime = Field(..., description="创建时间")
    started_at: datetime | None = Field(None, description="开始时间")
    completed_at: datetime | None = Field(None, description="完成时间")
    progress: float = Field(default=0.0, ge=0.0, le=1.0, description="进度（0.0-1.0）")
    file_name: str | None = Field(None, description="文件名")
    file_type: FileType | None = Field(None, description="文件类型")
    error: str | dict[str, Any] | None = Field(None, description="错误信息")


class AsyncParseResponse(BaseModel):
    """异步解析响应"""

    task_id: str = Field(..., description="任务ID")
    status_url: str = Field(..., description="状态查询URL")
    estimated_wait_time: int | None = Field(None, description="预计等待时间（秒）")


class HealthCheckResponse(BaseModel):
    """健康检查响应"""

    status: str = Field(..., description="状态：healthy, degraded, unhealthy")
    version: str = Field(..., description="服务版本")
    timestamp: datetime = Field(..., description="检查时间")
    details: dict[str, Any] = Field(default_factory=dict, description="详细状态信息")

"""Excel文档处理器"""
import asyncio
import traceback
from datetime import datetime
from pathlib import Path
from typing import List, Optional, Dict, Any

try:
    import openpyxl
    from openpyxl import Workbook, load_workbook
    from openpyxl.worksheet.worksheet import Worksheet
    EXCEL_AVAILABLE = True
except ImportError:
    EXCEL_AVAILABLE = False

from rag_document_parser.models.schemas import (
    ParseRequest,
    ParseResult,
    DocumentMetadata,
    PageContent,
    DocumentStructure,
    Heading,
    Paragraph,
    Table as TableModel,
    ImageInfo,
    BoundingBox,
    PageMetadata,
    FileType,
)
from rag_document_parser.services.exceptions import DependencyError, ParsingFailedError
from .base_processor import BaseOfficeProcessor


class ExcelDocumentProcessor(BaseOfficeProcessor):
    """Excel文档处理器"""

    def __init__(self):
        """初始化Excel处理器"""
        super().__init__("excel")
        self.supported_extensions = ["xlsx", "xls"]

    async def parse(
        self,
        file_path: Path,
        request: ParseRequest,
        request_id: str
    ) -> ParseResult:
        """解析Excel文档"""
        self.log_info(
            "开始解析Excel文档",
            extra={
                "request_id": request_id,
                "file_path": str(file_path),
            }
        )

        # 检查依赖可用性
        if not EXCEL_AVAILABLE:
            raise DependencyError(
                "openpyxl",
                "Excel文档处理需要openpyxl库，请安装openpyxl>=3.1.0"
            )

        try:
            # 异步执行解析（CPU密集型操作使用线程池）
            loop = asyncio.get_event_loop()
            parse_result = await loop.run_in_executor(
                None,
                self._parse_sync,
                file_path,
                request,
                request_id
            )

            self.log_info(
                "Excel文档解析完成",
                extra={
                    "request_id": request_id,
                    "file_path": str(file_path),
                    "sheets_count": len(parse_result.content)
                }
            )

            return parse_result

        except Exception as exc:
            self.log_error(
                "Excel文档解析失败",
                extra={
                    "request_id": request_id,
                    "file_path": str(file_path),
                    "error": str(exc),
                    "traceback": traceback.format_exc()
                }
            )
            raise ParsingFailedError(
                f"Excel文档解析失败: {str(exc)}",
                details={"engine": "excel", "format": "xlsx"}
            )

    def _parse_sync(
        self,
        file_path: Path,
        request: ParseRequest,
        request_id: str
    ) -> ParseResult:
        """同步解析Excel文档"""
        wb = None
        try:
            # 打开Excel工作簿（只读模式，提高性能）
            wb = load_workbook(
                filename=file_path,
                read_only=True,
                data_only=True  # 只读取值，不读取公式
            )

            # 提取文档元数据
            metadata = self._extract_metadata(wb, file_path)

            # 提取文档内容（每个工作表作为一个页面）
            pages_content = self._extract_content(wb, request, request_id)

            # 提取文档结构
            structure = self._extract_structure(wb, pages_content, request_id)

            # 创建解析结果
            result = ParseResult(
                content=pages_content,
                document_metadata=metadata,
                structure=structure,
            )

            return result

        except Exception as e:
            self.log_error(f"Excel文档解析失败: {e}")
            raise
        finally:
            if wb:
                wb.close()

    def _extract_metadata(self, wb: Workbook, file_path: Path) -> DocumentMetadata:
        """提取Excel文档元数据"""
        # 获取文件信息
        file_size = file_path.stat().st_size if file_path.exists() else None

        # 获取工作表数量
        sheet_count = len(wb.sheetnames)

        return DocumentMetadata(
            title=file_path.stem,
            author=None,  # Excel元数据中作者信息不易获取
            subject=None,
            keywords=[],
            created_date=datetime.fromtimestamp(file_path.stat().st_ctime) if file_path.exists() else None,
            modified_date=datetime.fromtimestamp(file_path.stat().st_mtime) if file_path.exists() else None,
            page_count=sheet_count,  # 每个工作表视为一页
            file_size=file_size,
            file_type=FileType.XLSX
        )

    def _extract_content(
        self,
        wb: Workbook,
        request: ParseRequest,
        request_id: str
    ) -> List[PageContent]:
        """提取Excel文档内容（每个工作表作为一个页面）"""
        pages_content = []

        for sheet_index, sheet_name in enumerate(wb.sheetnames):
            ws = wb[sheet_name]

            # 提取工作表数据
            data = self._extract_sheet_data(ws, request_id)

            # 将数据转换为文本表示
            text_content = self._convert_sheet_to_text(ws, data)

            # 提取表格（Excel本身就是表格，特殊处理）
            tables = []
            if request.extract_tables:
                table_model = TableModel(
                    table_index=sheet_index,
                    rows=data,
                    bbox=None
                )
                tables.append(table_model)

            # 页面元数据
            page_metadata = PageMetadata(
                dimensions={"width": 0, "height": 0},
                rotation=0,
                page_number=sheet_index + 1
            )

            page_content = PageContent(
                page=sheet_index + 1,
                text=text_content,
                tables=tables,
                images=[],  # Excel图片提取需要额外处理
                metadata=page_metadata
            )
            pages_content.append(page_content)

        return pages_content

    def _extract_sheet_data(
        self,
        ws: Worksheet,
        request_id: str
    ) -> List[List[str]]:
        """提取工作表数据"""
        data = []

        try:
            # 获取使用的单元格范围
            max_row = ws.max_row
            max_column = ws.max_column

            # 限制处理的行数，避免内存问题
            max_rows_to_process = 1000
            if max_row > max_rows_to_process:
                self.log_warning(
                    f"工作表 '{ws.title}' 行数过多 ({max_row}行)，仅处理前{max_rows_to_process}行",
                    extra={"request_id": request_id}
                )
                max_row = max_rows_to_process

            for row in ws.iter_rows(min_row=1, max_row=max_row, min_col=1, max_col=max_column):
                row_data = []
                for cell in row:
                    # 处理单元格值
                    cell_value = cell.value
                    if cell_value is None:
                        cell_text = ""
                    else:
                        cell_text = str(cell_value).strip()
                    row_data.append(cell_text)
                data.append(row_data)

        except Exception as exc:
            self.log_warning(
                "工作表数据提取失败",
                extra={
                    "request_id": request_id,
                    "sheet": ws.title,
                    "error": str(exc)
                }
            )

        return data

    def _convert_sheet_to_text(self, ws: Worksheet, data: List[List[str]]) -> str:
        """将工作表数据转换为文本"""
        lines = []

        # 添加工作表标题
        lines.append(f"工作表: {ws.title}")
        lines.append("=" * 50)

        if not data:
            lines.append("(空工作表)")
            return "\n".join(lines)

        # 添加表头（列字母）
        if data:
            header = [f"列{chr(65 + i)}" for i in range(len(data[0]))]
            lines.append(" | ".join(header))
            lines.append("-" * 50)

        # 添加数据行
        for i, row in enumerate(data, 1):
            # 限制单元格内容长度
            truncated_cells = []
            for cell in row:
                if len(cell) > 50:
                    truncated_cells.append(cell[:47] + "...")
                else:
                    truncated_cells.append(cell)
            row_text = " | ".join(truncated_cells)
            lines.append(f"行 {i}: {row_text}")

        return "\n".join(lines)

    def _extract_structure(
        self,
        wb: Workbook,
        pages_content: List[PageContent],
        request_id: str
    ) -> DocumentStructure:
        """提取Excel文档结构"""
        headings = []
        paragraphs = []

        try:
            # Excel没有传统意义上的标题和段落结构
            # 可以将工作表名称作为标题
            for sheet_index, sheet_name in enumerate(wb.sheetnames):
                headings.append(
                    Heading(
                        level=1,
                        text=sheet_name,
                        page=sheet_index + 1,
                        bbox=None
                    )
                )

            # 将工作表摘要作为段落
            for page_content in pages_content:
                if page_content.text:
                    # 提取前几行作为摘要
                    lines = page_content.text.split('\n')
                    summary = '\n'.join(lines[:5])  # 前5行作为摘要
                    paragraphs.append(
                        Paragraph(
                            text=summary,
                            page=page_content.page,
                            bbox=None
                        )
                    )

        except Exception as exc:
            self.log_warning(
                "Excel文档结构提取失败",
                extra={
                    "request_id": request_id,
                    "error": str(exc)
                }
            )

        return DocumentStructure(
            headings=headings,
            paragraphs=paragraphs
        )
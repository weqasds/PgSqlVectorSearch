using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;

namespace Infrastructure.InfrastructureShared.DocumentParser.Helpers;

/// <summary>
/// OpenXML 文档解析辅助类（用于 Excel 和 Word）
/// </summary>
internal static class OpenXmlParserHelper
{
    /// <summary>
    /// 共享字符串缓存管理器（用于 Excel）
    /// </summary>
    internal class SharedStringCache
    {
        private readonly Dictionary<int, string> _cache = new();
        private readonly SharedStringTablePart? _sharedStringPart;

        public SharedStringCache(SharedStringTablePart? sharedStringPart)
        {
            _sharedStringPart = sharedStringPart;
            BuildCache();
        }

        private void BuildCache()
        {
            if (_sharedStringPart?.SharedStringTable == null)
                return;

            var items = _sharedStringPart.SharedStringTable.Elements<DocumentFormat.OpenXml.Spreadsheet.SharedStringItem>();
            int index = 0;
            foreach (var item in items)
            {
                _cache[index] = item.InnerText;
                index++;
            }
        }

        public string? GetString(int index)
        {
            return _cache.TryGetValue(index, out var value) ? value : null;
        }

        public int Count => _cache.Count;
    }

    /// <summary>
    /// 流式读取 Excel 工作表行数据
    /// </summary>
    public static async IAsyncEnumerable<ExcelRow> ReadExcelRowsAsync(
        Stream stream,
        bool useStreaming,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var spreadsheet = SpreadsheetDocument.Open(stream, false);
        var workbookPart = spreadsheet.WorkbookPart;
        if (workbookPart == null)
            yield break;

        // 构建共享字符串缓存
        var sharedStringCache = new SharedStringCache(workbookPart.SharedStringTablePart);

        foreach (var worksheetPart in workbookPart.WorksheetParts)
        {
            var sheet = worksheetPart.Worksheet;
            var sheetData = sheet.GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.SheetData>();
            if (sheetData == null)
                continue;

            // 获取工作表名称
            var sheetName = GetSheetName(workbookPart, worksheetPart);

            foreach (var rowElement in sheetData.Elements<DocumentFormat.OpenXml.Spreadsheet.Row>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var row = new ExcelRow
                {
                    SheetName = sheetName,
                    RowIndex = (int)(rowElement.RowIndex?.Value ?? 0)
                };

                var cells = new List<string>();

                foreach (var cellElement in rowElement.Elements<DocumentFormat.OpenXml.Spreadsheet.Cell>())
                {
                    var cellValue = GetCellValue(cellElement, sharedStringCache);
                    cells.Add(cellValue);
                }

                row.Cells = cells.ToArray();
                yield return row;
            }
        }
    }

    /// <summary>
    /// 流式读取 Word 文档段落
    /// </summary>
    public static async IAsyncEnumerable<WordParagraph> ReadWordParagraphsAsync(
        Stream stream,
        bool useStreaming,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var wordDoc = WordprocessingDocument.Open(stream, false);
        var mainPart = wordDoc.MainDocumentPart;
        if (mainPart?.Document?.Body == null)
            yield break;

        foreach (var paragraphElement in mainPart.Document.Body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var text = paragraphElement.InnerText;
            if (string.IsNullOrWhiteSpace(text))
                continue;

            // 判断是否为标题（根据样式）
            var isHeading = IsHeadingParagraph(paragraphElement);

            yield return new WordParagraph
            {
                Text = text.Trim(),
                IsHeading = isHeading,
                Style = GetParagraphStyle(paragraphElement)
            };
        }
    }

    /// <summary>
    /// 分析 OpenXML 文档以确定最佳加载策略
    /// </summary>
    public static async Task<OpenXmlDocumentInfo> AnalyzeDocumentAsync(
        Stream stream,
        OpenXmlDocumentType documentType,
        CancellationToken cancellationToken)
    {
        try
        {
            if (documentType == OpenXmlDocumentType.Excel)
            {
                using var spreadsheet = SpreadsheetDocument.Open(stream, false);
                var workbookPart = spreadsheet.WorkbookPart;
                if (workbookPart == null)
                    return new OpenXmlDocumentInfo { ShouldUseStreaming = true };

                var worksheetCount = workbookPart.WorksheetParts.Count();
                var sharedStringCount = workbookPart.SharedStringTablePart?.SharedStringTable?.Count() ?? 0;

                // 检查工作表大小
                long totalCells = 0;
                foreach (var worksheetPart in workbookPart.WorksheetParts)
                {
                    var sheetData = worksheetPart.Worksheet.GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.SheetData>();
                    if (sheetData != null)
                    {
                        totalCells += sheetData.Elements<DocumentFormat.OpenXml.Spreadsheet.Row>()
                            .Sum(row => row.Elements<DocumentFormat.OpenXml.Spreadsheet.Cell>().Count());
                    }
                }

                return new OpenXmlDocumentInfo
                {
                    WorksheetCount = worksheetCount,
                    SharedStringCount = sharedStringCount,
                    TotalCells = totalCells,
                    ShouldUseStreaming = totalCells > 10000 || sharedStringCount > 1000
                };
            }
            else if (documentType == OpenXmlDocumentType.Word)
            {
                using var wordDoc = WordprocessingDocument.Open(stream, false);
                var mainPart = wordDoc.MainDocumentPart;
                if (mainPart?.Document?.Body == null)
                    return new OpenXmlDocumentInfo { ShouldUseStreaming = true };

                var paragraphCount = mainPart.Document.Body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>().Count();
                var hasComplexFormatting = CheckComplexFormatting(mainPart);

                return new OpenXmlDocumentInfo
                {
                    ParagraphCount = paragraphCount,
                    HasComplexFormatting = hasComplexFormatting,
                    ShouldUseStreaming = paragraphCount > 500 || hasComplexFormatting
                };
            }

            return new OpenXmlDocumentInfo { ShouldUseStreaming = true };
        }
        catch
        {
            // 如果分析失败，默认使用流式加载
            return new OpenXmlDocumentInfo { ShouldUseStreaming = true };
        }
    }

    /// <summary>
    /// 并行处理 Excel 工作表
    /// </summary>
    public static async Task<List<ExcelRow>> ProcessExcelInParallelAsync(
        Stream stream,
        int parallelism,
        CancellationToken cancellationToken)
    {
        var allRows = new List<ExcelRow>();

        // 由于 OpenXML 的限制，不能真正并行读取同一个流
        // 但可以并行处理不同的工作表部分（如果文件支持）
        // 这里简化实现，串行处理
        await foreach (var row in ReadExcelRowsAsync(stream, true, cancellationToken))
        {
            allRows.Add(row);
        }

        return allRows;
    }

    #region 私有辅助方法

    private static string GetCellValue(
        DocumentFormat.OpenXml.Spreadsheet.Cell cell,
        SharedStringCache sharedStringCache)
    {
        var cellValue = cell.CellValue?.Text;
        if (string.IsNullOrEmpty(cellValue))
            return string.Empty;

        // 如果是共享字符串引用
        if (cell.DataType?.Value == DocumentFormat.OpenXml.Spreadsheet.CellValues.SharedString)
        {
            if (int.TryParse(cellValue, out int index))
            {
                return sharedStringCache.GetString(index) ?? string.Empty;
            }
        }

        return cellValue;
    }

    private static string GetSheetName(
        WorkbookPart workbookPart,
        WorksheetPart worksheetPart)
    {
        var sheetId = workbookPart.GetIdOfPart(worksheetPart);
        var sheet = workbookPart.Workbook.Sheets?
            .Elements<DocumentFormat.OpenXml.Spreadsheet.Sheet>()
            .FirstOrDefault(s => s.Id?.Value == sheetId);

        return sheet?.Name?.Value ?? "Unknown";
    }

    private static bool IsHeadingParagraph(DocumentFormat.OpenXml.Wordprocessing.Paragraph paragraph)
    {
        var styleId = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        if (string.IsNullOrEmpty(styleId))
            return false;

        // 常见的标题样式标识
        return styleId.Contains("Heading", StringComparison.OrdinalIgnoreCase) ||
               styleId.StartsWith("Heading", StringComparison.OrdinalIgnoreCase) ||
               styleId.Contains("标题", StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetParagraphStyle(DocumentFormat.OpenXml.Wordprocessing.Paragraph paragraph)
    {
        return paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
    }

    private static bool CheckComplexFormatting(MainDocumentPart mainPart)
    {
        // 检查是否有复杂格式（表格、图片、复杂样式等）
        var tables = mainPart.Document.Body?.Elements<DocumentFormat.OpenXml.Wordprocessing.Table>();
        var images = mainPart.ImageParts;

        return (tables?.Any() ?? false) || (images?.Any() ?? false);
    }

    #endregion
}

#region 数据模型

/// <summary>
/// OpenXML 文档类型
/// </summary>
internal enum OpenXmlDocumentType
{
    Excel,
    Word
}

/// <summary>
/// Excel 行数据
/// </summary>
internal class ExcelRow
{
    public string SheetName { get; set; } = string.Empty;
    public int RowIndex { get; set; }
    public string[] Cells { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Word 段落数据
/// </summary>
internal class WordParagraph
{
    public string Text { get; set; } = string.Empty;
    public bool IsHeading { get; set; }
    public string? Style { get; set; }
}

/// <summary>
/// OpenXML 文档信息
/// </summary>
internal class OpenXmlDocumentInfo
{
    public int WorksheetCount { get; set; }
    public int SharedStringCount { get; set; }
    public long TotalCells { get; set; }
    public int ParagraphCount { get; set; }
    public bool HasComplexFormatting { get; set; }
    public bool ShouldUseStreaming { get; set; }

    public override string ToString()
    {
        if (WorksheetCount > 0)
        {
            return $"Excel文档: {WorksheetCount}个工作表, {TotalCells}个单元格, 共享字符串: {SharedStringCount}";
        }
        else
        {
            return $"Word文档: {ParagraphCount}个段落, 复杂格式: {HasComplexFormatting}";
        }
    }
}

#endregion
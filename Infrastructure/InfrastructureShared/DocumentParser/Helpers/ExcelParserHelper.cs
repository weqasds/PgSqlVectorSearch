using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Models.DocumentParsing;
using Application.Models.DocumentParsing.Node;
using DocumentFormat.OpenXml.Packaging;

namespace Infrastructure.InfrastructureShared.DocumentParser.Helpers;

/// <summary>
/// Excel 文档解析辅助类
/// </summary>
internal static class ExcelParserHelper
{
    /// <summary>
    /// 解析 Excel 文档为块
    /// </summary>
    public static async Task<IReadOnlyList<ParsedBlock>> ParseToBlocksAsync(
        Stream stream,
        string fileName,
        DocumentParseOptions? options,
        CancellationToken cancellationToken)
    {
        // 分析文档以确定最佳策略
        var docInfo = await OpenXmlParserHelper.AnalyzeDocumentAsync(
            stream, OpenXmlDocumentType.Excel, cancellationToken).ConfigureAwait(false);

        var useStreaming = DocumentLoadStrategy.ShouldUseStreaming(stream) || docInfo.ShouldUseStreaming;
        var blocks = new List<ParsedBlock>();

        // 重置流位置
        if (stream.CanSeek && stream.Position != 0)
            stream.Position = 0;

        try
        {
            await foreach (var row in OpenXmlParserHelper.ReadExcelRowsAsync(stream, useStreaming, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                // 将行转换为文本块
                var rowText = string.Join("\t", row.Cells);
                if (!string.IsNullOrWhiteSpace(rowText))
                {
                    // 如果是第一行或工作表名称变化，添加标题块
                    if (row.RowIndex == 1)
                    {
                        blocks.Add(ParsedBlock.CreateTitleBlock($"工作表: {row.SheetName}"));
                    }

                    blocks.Add(ParsedBlock.CreateTableBlock(rowText));
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException($"解析 Excel 文件 '{fileName}' 失败: {ex.Message}", ex);
        }

        return blocks;
    }

    /// <summary>
    /// 解析 Excel 文档为结构化文档
    /// </summary>
    public static async Task<StructuredDocument> ParseToStructuredAsync(
        Stream stream,
        string fileName,
        DocumentParseOptions? options,
        CancellationToken cancellationToken)
    {
        // 分析文档以确定最佳策略
        var docInfo = await OpenXmlParserHelper.AnalyzeDocumentAsync(
            stream, OpenXmlDocumentType.Excel, cancellationToken).ConfigureAwait(false);

        var useStreaming = DocumentLoadStrategy.ShouldUseStreaming(stream) || docInfo.ShouldUseStreaming;
        var structuredDoc = new StructuredDocument
        {
            Metadata = new Dictionary<string, object>
            {
                ["FileName"] = fileName,
                ["WorksheetCount"] = docInfo.WorksheetCount,
                ["TotalCells"] = docInfo.TotalCells,
                ["SharedStringCount"] = docInfo.SharedStringCount,
                ["LoadStrategy"] = useStreaming ? "Streaming" : "DOM"
            },
            Nodes = new List<DocumentNode>()
        };

        // 重置流位置
        if (stream.CanSeek && stream.Position != 0)
            stream.Position = 0;

        try
        {
            var currentSheetName = string.Empty;
            TableNode? currentTable = null;
            var currentSheetNodes = new List<DocumentNode>();

            await foreach (var row in OpenXmlParserHelper.ReadExcelRowsAsync(stream, useStreaming, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                // 检测工作表变化
                if (currentSheetName != row.SheetName)
                {
                    // 完成前一个工作表
                    if (currentTable != null && currentTable.Rows.Count > 0)
                    {
                        currentSheetNodes.Add(currentTable);
                        currentTable = null;
                    }

                    if (!string.IsNullOrEmpty(currentSheetName) && currentSheetNodes.Count > 0)
                    {
                        var sheetNode = new HeadingNode
                        {
                            Level = 2,
                            Text = $"工作表: {currentSheetName}",
                            Children = currentSheetNodes.ToList()
                        };
                        structuredDoc.Nodes.Add(sheetNode);
                    }

                    // 开始新工作表
                    currentSheetName = row.SheetName;
                    currentSheetNodes.Clear();

                    // 添加工作表标题
                    var sheetHeading = new HeadingNode
                    {
                        Level = 2,
                        Text = $"工作表: {currentSheetName}"
                    };
                    currentSheetNodes.Add(sheetHeading);
                }

                // 处理行数据
                if (row.RowIndex == 1)
                {
                    // 第一行可能是表头，开始新表格
                    if (currentTable != null && currentTable.Rows.Count > 0)
                    {
                        currentSheetNodes.Add(currentTable);
                    }

                    currentTable = new TableNode
                    {
                        Headers = row.Cells.ToList(),
                        Rows = new()
                    };
                }
                else if (currentTable != null)
                {
                    // 添加数据行
                    currentTable.Rows.Add(row.Cells.ToList());
                }
                else
                {
                    // 如果没有表格，直接添加为文本
                    var rowText = string.Join(" | ", row.Cells);
                    if (!string.IsNullOrWhiteSpace(rowText))
                    {
                        var paragraph = new ParagraphNode
                        {
                            Text = rowText
                        };
                        currentSheetNodes.Add(paragraph);
                    }
                }
            }

            // 处理最后一个工作表和表格
            if (currentTable != null && currentTable.Rows.Count > 0)
            {
                currentSheetNodes.Add(currentTable);
            }

            if (!string.IsNullOrEmpty(currentSheetName) && currentSheetNodes.Count > 0)
            {
                var sheetNode = new HeadingNode
                {
                    Level = 2,
                    Text = $"工作表: {currentSheetName}",
                    Children = currentSheetNodes.ToList()
                };
                structuredDoc.Nodes.Add(sheetNode);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException($"结构化解析 Excel 文件 '{fileName}' 失败: {ex.Message}", ex);
        }

        return structuredDoc;
    }

    /// <summary>
    /// 并行解析 Excel 文档
    /// </summary>
    public static async Task<IReadOnlyList<ParsedBlock>> ParseToBlocksParallelAsync(
        Stream stream,
        string fileName,
        DocumentParseOptions? options,
        CancellationToken cancellationToken)
    {
        // 检查是否应该使用并行处理
        if (!DocumentLoadStrategy.ShouldUseParallelProcessing(stream))
        {
            return await ParseToBlocksAsync(stream, fileName, options, cancellationToken).ConfigureAwait(false);
        }

        // 重置流位置
        if (stream.CanSeek && stream.Position != 0)
            stream.Position = 0;

        var parallelism = DocumentLoadStrategy.GetOptimalParallelism(stream);

        // 由于 OpenXML 限制，不能真正并行读取同一个流
        // 这里使用串行处理，但未来可以优化为多流读取
        return await ParseToBlocksAsync(stream, fileName, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 快速提取 Excel 元数据
    /// </summary>
    public static async Task<Dictionary<string, object>> ExtractMetadataAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        var metadata = new Dictionary<string, object>();

        try
        {
            // 重置流位置
            if (stream.CanSeek && stream.Position != 0)
                stream.Position = 0;

            using var spreadsheet = SpreadsheetDocument.Open(stream, false);
            var workbookPart = spreadsheet.WorkbookPart;
            if (workbookPart == null)
                return metadata;

            metadata["WorksheetCount"] = workbookPart.WorksheetParts.Count();
            metadata["HasSharedStrings"] = workbookPart.SharedStringTablePart != null;

            if (workbookPart.SharedStringTablePart?.SharedStringTable != null)
            {
                metadata["SharedStringCount"] = workbookPart.SharedStringTablePart.SharedStringTable.Count();
            }

            // 获取工作表名称
            var sheetNames = new List<string>();
            var sheets = workbookPart.Workbook.Sheets?.Elements<DocumentFormat.OpenXml.Spreadsheet.Sheet>();
            if (sheets != null)
            {
                foreach (var sheet in sheets)
                {
                    if (sheet.Name?.Value != null)
                    {
                        sheetNames.Add(sheet.Name.Value);
                    }
                }
            }
            metadata["SheetNames"] = sheetNames;

            return metadata;
        }
        catch
        {
            // 如果提取元数据失败，返回空字典
            return metadata;
        }
    }
}
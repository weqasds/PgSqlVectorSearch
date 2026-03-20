using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Application.ApplicationServiceContract.DocumentParsing;
using Application.Models.DocumentParsing;

namespace Infrastructure.InfrastructureShared.DocumentParser;

/// <summary>
/// Excel 文档解析器（占位符实现）。
/// 需要安装 OpenXML SDK 或 EPPlus 等 Excel 解析库才能实际使用。
/// </summary>
public class ExcelDocumentParser : ChunkableDocumentParserBase
{
    /// <inheritdoc />
    protected internal override IReadOnlySet<string> SupportedExtensions { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".xlsx",
        ".xls",
        ".csv",
        ".tsv"
    };

    /// <inheritdoc />
    protected override Task<IReadOnlyList<ParsedBlock>> ParseBlocksCoreAsync(
        Stream stream,
        string fileName,
        DocumentParseOptions? options,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException(
            "Excel 解析需要安装 OpenXML SDK 或 EPPlus 等库。请安装相应的 NuGet 包，并实现实际的解析逻辑。");
    }

    /// <summary>
    /// 示例实现（需要取消注释并安装相应的 NuGet 包）：
    /// </summary>
    /*
    protected override async Task<IReadOnlyList<ParsedBlock>> ParseBlocksCoreAsync(
        Stream stream,
        string fileName,
        DocumentParseOptions? options,
        CancellationToken cancellationToken)
    {
        var blocks = new List<ParsedBlock>();

        // 使用 EPPlus 示例（需要安装 EPPlus 包）：
        // using OfficeOpenXml;
        //
        // using var package = new ExcelPackage(stream);
        // var workbook = package.Workbook;
        // foreach (var worksheet in workbook.Worksheets)
        // {
        //     // 将工作表名称作为标题
        //     blocks.Add(CreateTitleBlock($"工作表: {worksheet.Name}"));
        //
        //     // 遍历所有有数据的单元格
        //     var dimension = worksheet.Dimension;
        //     if (dimension != null)
        //     {
        //         for (int row = dimension.Start.Row; row <= dimension.End.Row; row++)
        //         {
        //             var rowValues = new List<string>();
        //             for (int col = dimension.Start.Column; col <= dimension.End.Column; col++)
        //             {
        //                 var cell = worksheet.Cells[row, col];
        //                 rowValues.Add(cell.Text);
        //             }
        //             var rowText = string.Join("\t", rowValues);
        //             if (!string.IsNullOrWhiteSpace(rowText))
        //             {
        //                 blocks.Add(CreateTextBlock(rowText, BlockType.Table));
        //             }
        //         }
        //     }
        // }

        return blocks;
    }
    */
}
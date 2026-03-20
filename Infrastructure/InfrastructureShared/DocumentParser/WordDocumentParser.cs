using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Application.ApplicationServiceContract.DocumentParsing;
using Application.Models.DocumentParsing;

namespace Infrastructure.InfrastructureShared.DocumentParser;

/// <summary>
/// Word 文档解析器（占位符实现）。
/// 需要安装 OpenXML SDK 或其他 Word 解析库才能实际使用。
/// </summary>
public class WordDocumentParser : ChunkableDocumentParserBase
{
    /// <inheritdoc />
    public override IReadOnlySet<string> SupportedExtensions { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".docx",
        ".doc"
    };

    /// <inheritdoc />
    protected override Task<IReadOnlyList<ParsedBlock>> ParseBlocksCoreAsync(
        Stream stream,
        string fileName,
        DocumentParseOptions? options,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException(
            "Word 解析需要安装 OpenXML SDK 或其他 Word 解析库。请安装 DocumentFormat.OpenXml 等 NuGet 包，并实现实际的解析逻辑。");
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

        // 使用 OpenXML SDK 示例（需要安装 DocumentFormat.OpenXml 包）：
        // using DocumentFormat.OpenXml.Packaging;
        // using DocumentFormat.OpenXml.Wordprocessing;
        //
        // using var wordDoc = WordprocessingDocument.Open(stream, false);
        // var body = wordDoc.MainDocumentPart?.Document.Body;
        // if (body != null)
        // {
        //     foreach (var paragraph in body.Elements<Paragraph>())
        //     {
        //         var text = paragraph.InnerText;
        //         if (!string.IsNullOrWhiteSpace(text))
        //         {
        //             // 简单判断是否为标题（根据样式）
        //             var isTitle = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value?.Contains("Heading") == true;
        //             var blockType = isTitle ? BlockType.Title : BlockType.Paragraph;
        //             blocks.Add(CreateTextBlock(text.Trim(), blockType));
        //         }
        //     }
        // }

        return blocks;
    }
    */
}
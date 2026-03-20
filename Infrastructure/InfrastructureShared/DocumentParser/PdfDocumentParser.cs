using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Application.ApplicationServiceContract.DocumentParsing;
using Application.Models.DocumentParsing;

namespace Infrastructure.InfrastructureShared.DocumentParser;

/// <summary>
/// PDF 文档解析器（占位符实现）。
/// 需要安装 PDF 解析库（如 iText7、PdfPig 或 PDFium）才能实际使用。
/// </summary>
public class PdfDocumentParser : ChunkableDocumentParserBase
{
    /// <inheritdoc />
    protected internal override IReadOnlySet<string> SupportedExtensions { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf"
    };

    /// <inheritdoc />
    protected override Task<IReadOnlyList<ParsedBlock>> ParseBlocksCoreAsync(
        Stream stream,
        string fileName,
        DocumentParseOptions? options,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException(
            "PDF 解析需要安装 PDF 解析库。请安装 iText7、PdfPig 或 PDFium 等 NuGet 包，并实现实际的解析逻辑。");
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

        // 使用 iText7 示例（需要安装 itext7 包）：
        // using iText.Kernel.Pdf;
        // using iText.Kernel.Pdf.Canvas.Parser;
        // using iText.Kernel.Pdf.Canvas.Parser.Listener;
        //
        // var pdfReader = new PdfReader(stream);
        // var pdfDoc = new PdfDocument(pdfReader);
        //
        // for (int page = 1; page <= pdfDoc.GetNumberOfPages(); page++)
        // {
        //     var strategy = new SimpleTextExtractionStrategy();
        //     var text = PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(page), strategy);
        //     if (!string.IsNullOrWhiteSpace(text))
        //     {
        //         blocks.Add(CreateTextBlock(text.Trim(), BlockType.Paragraph, page));
        //     }
        // }
        //
        // pdfDoc.Close();
        // pdfReader.Close();

        return blocks;
    }
    */
}
using Application.ApplicationServiceContract.DocumentParsing;
using Application.Models.DocumentParsing;
using DomainService.Exceptions;

namespace Infrastructure.InfrastructureShared.DocumentParser;

/// <summary>
/// PDF 文档解析器（占位符实现）。
/// 需要安装 PDF 解析库（如 iText7、PdfPig 或 PDFium）才能实际使用。
/// </summary>
public class PdfDocumentParser : DocumentParserBase, IChunkableDocumentParser, IStructuredDocumentParser
{
    /// <inheritdoc />
    protected internal override IReadOnlySet<string> SupportedExtensions { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf"
    };
    /// <inheritdoc />
    protected override async Task<ParsedDocument> ParseCoreAsync(
        Stream stream,
        string fileName,
        DocumentParseOptions? options,
        CancellationToken cancellationToken)
    {
        // 默认使用结构化解析
        var structured = await ParseStructuredCoreAsync(stream, fileName, options, cancellationToken);
        return structured.ConvertToParsedDocument();
    }
    /// <inheritdoc />
    public async Task<IReadOnlyList<ParsedBlock>> ParseBlocksAsync(
        Stream stream,
        string fileName,
        DocumentParseOptions? documentParseOptions = null,
        CancellationToken cancellationToken = default)
    {
        ValidateArguments(stream, fileName);

        if (!CanHandle(fileName))
        {
            throw new DocumentFormatNotSupportedException(
                $"文件 '{fileName}' 的格式不被支持。本解析器支持: {string.Join(", ", SupportedExtensions)}")
            {
                FileName = fileName,
                SupportedFormats = string.Join(", ", SupportedExtensions)
            };
        }

        try
        {
            return await ParseBlocksCoreAsync(stream, fileName, documentParseOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DocumentFormatNotSupportedException)
        {
            throw;
        }
        catch (DocumentParsingFailedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new DocumentParsingFailedException(
                $"解析文件 '{fileName}' 为块时发生错误。", ex)
            {
                FileName = fileName,
                ParserType = GetType().Name
            };
        }
    }

    /// <summary>
    /// 将文档解析为多个语义块的实现。
    /// </summary>
    protected virtual async Task<IReadOnlyList<ParsedBlock>> ParseBlocksCoreAsync(
        Stream stream,
        string fileName,
        DocumentParseOptions? options,
        CancellationToken cancellationToken)
    {
        return await Helpers.PdfParserHelper.ParseToBlocksAsync(stream, fileName, options, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<StructuredDocument> ParseStructuredAsync(
        Stream stream,
        string fileName,
        DocumentParseOptions? documentParseOptions = null,
        CancellationToken cancellationToken = default)
    {
        ValidateArguments(stream, fileName);

        if (!CanHandle(fileName))
        {
            throw new DocumentFormatNotSupportedException(
                $"文件 '{fileName}' 的格式不被支持。本解析器支持: {string.Join(", ", SupportedExtensions)}")
            {
                FileName = fileName,
                SupportedFormats = string.Join(", ", SupportedExtensions)
            };
        }

        try
        {
            return await ParseStructuredCoreAsync(stream, fileName, documentParseOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DocumentFormatNotSupportedException)
        {
            throw;
        }
        catch (DocumentParsingFailedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new DocumentParsingFailedException(
                $"结构化解析文件 '{fileName}' 时发生错误。", ex)
            {
                FileName = fileName,
                ParserType = GetType().Name
            };
        }
    }

    /// <summary>
    /// PDF结构化解析文档
    /// </summary>
    /// <param name="stream">文件流</param>
    /// <param name="fileName">文件名</param>
    /// <param name="documentParseOptions">文档选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结构化文档</returns>
    /// <exception cref="DocumentFormatNotSupportedException"></exception>
    /// <exception cref="DocumentParsingFailedException"></exception>
    protected virtual async Task<StructuredDocument> ParseStructuredCoreAsync(
        Stream stream,
        string fileName,
        DocumentParseOptions? options,
        CancellationToken cancellationToken)
    {
        return await Helpers.PdfParserHelper.ParseToStructuredAsync(stream, fileName, options, cancellationToken)
            .ConfigureAwait(false);
    }
}
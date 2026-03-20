using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Application.ApplicationServiceContract.DocumentParsing;
using Application.Models.DocumentParsing;
using DomainService.Exceptions;

namespace Infrastructure.InfrastructureShared.DocumentParser;

/// <summary>
/// 文档解析器基类，提供通用验证和扩展名检查。
/// </summary>
public abstract class DocumentParserBase : IDocumentParser
{
    /// <summary>
    /// 支持的文件扩展名（小写，包含点），例如 ".txt", ".pdf"。
    /// </summary>
    protected internal abstract IReadOnlySet<string> SupportedExtensions { get; }

    /// <summary>
    /// 实际解析文档的实现。
    /// </summary>
    protected abstract Task<ParsedDocument> ParseCoreAsync(
        Stream stream,
        string fileName,
        DocumentParseOptions? options,
        CancellationToken cancellationToken);

    /// <inheritdoc />
    public virtual bool CanHandle(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return SupportedExtensions.Contains(extension);
    }

    /// <inheritdoc />
    public async Task<ParsedDocument> ParseAsync(
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
            return await ParseCoreAsync(stream, fileName, documentParseOptions, cancellationToken)
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
                $"解析文件 '{fileName}' 时发生错误。", ex)
            {
                FileName = fileName,
                ParserType = GetType().Name
            };
        }
    }

    protected static void ValidateArguments(Stream stream, string fileName)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        if (fileName == null)
            throw new ArgumentNullException(nameof(fileName));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("文件名不能为空或空白字符串。", nameof(fileName));
        if (!stream.CanRead)
            throw new ArgumentException("流必须可读。", nameof(stream));
    }
}
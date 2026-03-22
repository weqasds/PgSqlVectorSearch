using Application.Models.DocumentParsing;

namespace Application.ApplicationServiceContract.DocumentParsing;
public interface IStructuredDocumentParser : IDocumentParser
{
    /// <summary>
    /// 结构化解析文档
    /// </summary>
    /// <param name="stream">文件流</param>
    /// <param name="fileName">文件名</param>
    /// <param name="documentParseOptions">文档选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结构化文档</returns>
    /// <exception cref="DocumentFormatNotSupportedException">文档格式不支持</exception>
    /// <exception cref="DocumentParsingFailedException">文档解析失败</exception>
    Task<StructuredDocument> ParseStructuredAsync(
        Stream stream,
        string fileName,
        DocumentParseOptions? documentParseOptions = null,
        CancellationToken cancellationToken = default);
}
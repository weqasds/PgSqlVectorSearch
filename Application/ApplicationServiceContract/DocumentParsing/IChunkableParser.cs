
using Application.Models.DocumentParsing;

namespace Application.ApplicationServiceContract.DocumentParsing;

/// <summary>
/// 支持“分块解析”的文档解析器。
/// </summary>
/// <remarks>
/// 适用于：
/// - RAG（Embedding 切分）
/// - 长文本分段处理
/// - 流式处理
/// </remarks>
public interface IChunkableDocumentParser : IDocumentParser
{
    /// <summary>
    /// 将文档解析为多个语义块（如段落、标题、表格等）。
    /// </summary>
    /// <param name="stream">文件流。</param>
    /// <param name="fileName">文件名。</param>
    /// <param name="documentParseOptions">解析选项。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>解析后的块集合。</returns>
    Task<IReadOnlyList<ParsedBlock>> ParseBlocksAsync(
        Stream stream,
        string fileName,
        DocumentParseOptions? documentParseOptions = null,
        CancellationToken cancellationToken = default);
}
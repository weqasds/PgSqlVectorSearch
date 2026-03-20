using System.Diagnostics.CodeAnalysis; // 用于静态代码分析特性
using System.IO; // 提供 Stream
using System.Threading; // 提供 CancellationToken
using System.Threading.Tasks;
using Application.Models.DocumentParsing; // 提供 Task 及相关类型
using DomainService.Exceptions; // 提供自定义异常

namespace Application.ApplicationServiceContract.DocumentParsing;
/// <summary>
/// 文档解析接口
/// </summary>
public interface IDocumentParser
{
    /// <summary>
    /// 异步解析提供的文档流。
    /// </summary>
    /// <param name="stream">包含文档数据的流。调用方负责该流生命周期的管理。</param>
    /// <param name="fileName">原始文件名，可用于确定解析策略（如通过扩展名）或丰富输出元数据。</param>
    /// <param name="cancellationToken">用于传播取消操作通知的令牌。</param>
    /// <returns>一个表示异步解析操作的任务。任务的结果是解析出的文档文本内容。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="stream"/> 或 <paramref name="fileName"/> 为 null 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="fileName"/> 为空或空白字符串时抛出。</exception>
    /// <exception cref="DocumentFormatNotSupportedException">当文档格式不被支持时抛出。(自定义领域异常)</exception>
    /// <exception cref="DocumentParsingFailedException">当解析过程因内容问题失败时抛出。(自定义领域异常)</exception>
    /// <exception cref="OperationCanceledException">当操作被 <paramref name="cancellationToken"/> 取消时抛出。</exception>
    [return: MaybeNull]
    Task<ParsedDocument> ParseAsync(
        Stream stream,
        string fileName,
        DocumentParseOptions? documentParseOptions = null,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// 判断当前解析器是否可以处理指定文件。
    /// </summary>
    /// <param name="fileName">文件名（通常通过扩展名判断）。</param>
    /// <returns>是否支持。</returns>
    bool CanHandle(string fileName);
}


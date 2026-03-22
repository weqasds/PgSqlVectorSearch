namespace Application.ApplicationServiceContract.DocumentParsing;
/// <summary>
/// 文档解析器选择器接口。
/// </summary>
public interface IDocumentParserSelector
{
    /// <summary>
    /// 获取能够处理指定文件的文档解析器。
    /// </summary>
    /// <param name="fileName">文件名。</param>
    /// <returns>文档解析器实例。</returns>
    /// <exception cref="DocumentFormatNotSupportedException">没有找到支持的解析器时抛出。</exception>
    IDocumentParser GetParser(string fileName);

    /// <summary>
    /// 获取能够处理指定文件的分块文档解析器（如果存在）。
    /// </summary>
    /// <param name="fileName">文件名。</param>
    /// <returns>分块文档解析器，如果不存在则返回 null。</returns>
    IChunkableDocumentParser? GetChunkableParser(string fileName);

    /// <summary>
    /// 获取能够处理指定文件的结构化文档解析器（如果存在）。
    /// </summary>
    /// <param name="fileName">文件名。</param>
    /// <returns>结构化文档解析器，如果不存在则返回 null。</returns>
    IStructuredDocumentParser? GetStructuredParser(string fileName);
}
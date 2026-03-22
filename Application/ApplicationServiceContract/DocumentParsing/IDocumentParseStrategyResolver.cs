using Application.Models.DocumentParsing;

namespace Application.ApplicationServiceContract.DocumentParsing;

/// <summary>
/// 文档解析策略解析器接口
/// 负责根据文档特征决定解析模式（Blocks 或 Structured）
/// </summary>
public interface IDocumentParseStrategyResolver
{
    /// <summary>
    /// 解析文档并确定解析模式
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <param name="fileSize">文件大小（可选）</param>
    /// <param name="contentType">内容类型（可选）</param>
    /// <returns>文档解析模式</returns>
    DocumentParseMode Resolve(string fileName, long? fileSize = null, string? contentType = null);
}
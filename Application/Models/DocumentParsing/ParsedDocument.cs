namespace Application.Models.DocumentParsing;
/// <summary>
/// 文档解析后的统一表示模型。
/// </summary>
/// <remarks>
/// 设计目标：
/// - 提供完整文本（FullText）
/// - 提供分块结构（Blocks）
/// - 提供扩展元数据（Metadata）
/// </remarks>
public class ParsedDocument
{
    /// <summary>
    /// 完整文本
    /// </summary>
    public string FullText { get; set; } = default!;
    /// <summary>
    /// 文档的分块结构
    /// </summary>
    public List<ParsedBlock> Blocks { get; set; } = new();
    /// <summary>
    /// 文档元数据
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}


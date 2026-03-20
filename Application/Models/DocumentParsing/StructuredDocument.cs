using Application.Models.DocumentParsing.Node;
namespace Application.Models.DocumentParsing;
/// <summary>
/// 表示一个结构化文档（带层级语义）
/// </summary>
public class StructuredDocument
{
    /// <summary>
    /// 文档标题
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 文档节点树（核心）
    /// </summary>
    public List<DocumentNode> Nodes { get; set; } = new();

    /// <summary>
    /// 元数据（作者、页数等）
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}
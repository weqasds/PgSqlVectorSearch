namespace Application.Models.DocumentParsing.Node;
/// <summary>
/// 文档节点抽象（支持树结构）
/// </summary>
public abstract class DocumentNode
{
    /// <summary>
    /// 子节点（用于层级结构，如标题 -> 段落）
    /// </summary>
    public List<DocumentNode> Children { get; set; } = new();

    /// <summary>
    /// 页码
    /// </summary>
    public int? PageNumber { get; set; }

    /// <summary>
    /// 节点元数据
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}
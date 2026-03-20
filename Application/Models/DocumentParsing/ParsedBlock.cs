namespace Application.Models.DocumentParsing;
public enum BlockType
{
    Paragraph,
    Title,
    Table
}
public class ParsedBlock
{
    /// <summary>
    /// 文本内容
    /// </summary>
    public string Text { get; set; } = default!;
    /// <summary>
    /// 块类型
    /// </summary>
    public BlockType? Type { get; set; } // paragraph / title / table
    /// <summary>
    /// 页码
    /// </summary>
    public int? PageNumber { get; set; }
    /// <summary>
    /// 块元数据
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}
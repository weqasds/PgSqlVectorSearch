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
    /// <summary>
    /// 实用方法：创建文本块。
    /// </summary>
    public static ParsedBlock CreateTextBlock(string text, BlockType? type = null, int? pageNumber = null)
    {
        return new ParsedBlock
        {
            Text = text,
            Type = type ?? BlockType.Paragraph,
            PageNumber = pageNumber
        };
    }
    /// <summary>
    /// 实用方法：创建表格块。
    /// </summary>
    public static ParsedBlock CreateTableBlock(string text, int? pageNumber = null)
    {
        return new ParsedBlock
        {
            Text = text,
            Type = BlockType.Table,
            PageNumber = pageNumber
        };
    }
    /// <summary>
    /// 实用方法：创建标题块。
    /// </summary>
    public static ParsedBlock CreateTitleBlock(string text, int? pageNumber = null)
    {
        return new ParsedBlock
        {
            Text = text,
            Type = BlockType.Title,
            PageNumber = pageNumber
        };
    }
}
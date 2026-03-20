namespace Application.Models.DocumentParsing.Node;
public class TableNode : DocumentNode
{
    /// <summary>
    /// 表格数据（行列结构）
    /// </summary>
    public List<List<string>> Rows { get; set; } = new();

    /// <summary>
    /// 表头（可选）
    /// </summary>
    public List<string>? Headers { get; set; }
}
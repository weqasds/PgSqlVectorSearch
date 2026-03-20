namespace Application.Models.DocumentParsing.Node; 
public class HeadingNode : DocumentNode
{
    /// <summary>
    /// 标题文本
    /// </summary>
    public string Text { get; set; } = default!;

    /// <summary>
    /// 标题级别（H1-H6）
    /// </summary>
    public int Level { get; set; }
}
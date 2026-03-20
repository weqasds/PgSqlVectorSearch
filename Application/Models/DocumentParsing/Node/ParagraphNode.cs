namespace Application.Models.DocumentParsing.Node;
public class ParagraphNode : DocumentNode
{
    /// <summary>
    /// 段落文本
    /// </summary>
    public string Text { get; set; } = default!;
}
namespace Application.Models.DocumentParsing;

/// <summary>
/// 文档解析参数（仅用于控制解析行为细节，不包含策略决策）
/// </summary>
public class DocumentParseOptions
{
    /// <summary>
    /// 起始页（包含）
    /// </summary>
    public int StartPage { get; set; } = 1;

    /// <summary>
    /// 结束页（包含）
    /// </summary>
    public int? EndPage { get; set; }

    /// <summary>
    /// 是否保留布局信息（如段落结构、换行等）
    /// </summary>
    public bool PreserveLayout { get; set; } = true;

    /// <summary>
    /// 是否提取表格
    /// </summary>
    public bool ExtractTables { get; set; } = true;

    /// <summary>
    /// 是否提取图像
    /// </summary>
    public bool ExtractImages { get; set; } = false;
}

/// <summary>
/// 文档解析模式（业务策略决策）
/// </summary>
public enum DocumentParseMode
{
    /// <summary>
    /// 解析为文本块
    /// </summary>
    Blocks,

    /// <summary>
    /// 解析为结构化文档
    /// </summary>
    Structured
}
namespace Application.Models.DocumentParsing;
public class DocumentParseOptions
{
    /// <summary>
    /// 起始页
    /// </summary>
    public int StartPage { get; set; }
    /// <summary>
    /// 结束页
    /// </summary>
    public int EndPage { get; set; }
    /// <summary>
    /// 是否保留布局信息
    /// </summary>
    public bool PreserveLayout { get; set; } = true;
    /// <summary>
    /// 是否解析表格
    /// </summary>
    public bool ExtractTables { get; set; } = true;
    /// <summary>
    /// 是否解析图像
    /// </summary>
    public bool ExtractImages { get; set; } = false;
}
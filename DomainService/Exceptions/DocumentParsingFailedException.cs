namespace DomainService.Exceptions;

/// <summary>
/// 文档解析失败异常。
/// </summary>
public class DocumentParsingFailedException : Exception
{
    public DocumentParsingFailedException()
    {
    }

    public DocumentParsingFailedException(string message)
        : base(message)
    {
    }

    public DocumentParsingFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public string? FileName { get; init; }
    public string? ParserType { get; init; }
}
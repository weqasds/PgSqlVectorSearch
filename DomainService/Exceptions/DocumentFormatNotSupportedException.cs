namespace DomainService.Exceptions;

/// <summary>
/// 文档格式不支持异常。
/// </summary>
public class DocumentFormatNotSupportedException : Exception
{
    public DocumentFormatNotSupportedException()
    {
    }

    public DocumentFormatNotSupportedException(string message)
        : base(message)
    {
    }

    public DocumentFormatNotSupportedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public string? FileName { get; init; }
    public string? SupportedFormats { get; init; }
}
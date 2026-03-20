
namespace Application.Extensions;
public static class MimeTypeMapper
{
    public static FileType GetDocumentType(string contentType)
    {
        return contentType switch
        {
            "text/plain" => FileType.Text,
            "text/markdown" => FileType.Markdown,
            "application/json" => FileType.Json,
            "application/xml" => FileType.Xml,
            "text/html" => FileType.Html,
            "application/pdf" => FileType.Pdf,
            "application/msword" or "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => FileType.Word,
            "application/vnd.ms-excel" or "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => FileType.Excel,
            "application/vnd.ms-powerpoint" or "application/vnd.openxmlformats-officedocument.presentationml.presentation" => FileType.PowerPoint,
            "image/jpeg" or "image/png" or "image/gif" or "image/bmp" => FileType.Image,
            "application/zip" or 
            "application/x-rar-compressed" or 
            "application/x-7z-compressed" or
            "application/x-zip-compressed" or 
            "application/x-gzip"
                     => FileType.Zip,
            _ => FileType.Unknown
        };
    }
    public static string GetMimeType(this FileType fileType)
    {
        return fileType switch
        {
            FileType.Text => "text/plain",
            FileType.Markdown => "text/markdown",
            FileType.Json => "application/json",
            FileType.Xml => "application/xml",
            FileType.Html => "text/html",
            FileType.Pdf => "application/pdf",
            FileType.Word => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            FileType.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            FileType.PowerPoint => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            FileType.Image => "image/jpeg", // 默认使用JPEG
            FileType.Zip => "application/zip",
            _ => "application/octet-stream"
        };
    }
}
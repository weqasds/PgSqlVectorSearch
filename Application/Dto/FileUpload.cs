using System.IO;

namespace Application.ApplicationServiceContract;

public class FileUpload
{
    public Stream Content { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long FileSize { get; set; }
}
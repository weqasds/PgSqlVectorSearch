using System.IO;

namespace Application.ApplicationServiceContract;

public class FileUpload
{
    public Stream Content { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public FileType ContentType { get; set; }
    public long FileSize { get; set; }
}
namespace Application.ApplicationServiceContract;

public class UploadDocumentRequest
{
    public FileUpload File { get; set; } = null!;
    public string FileName { get; set; } = null!;
}
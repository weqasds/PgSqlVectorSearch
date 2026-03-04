using System.IO;
using System.Threading.Tasks;

namespace DomainService.ServiceContract;

public interface IFileStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
    Task<Stream> DownloadFileAsync(string fileId);
    Task DeleteFileAsync(string fileId);
}
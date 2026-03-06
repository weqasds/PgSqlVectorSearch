using System.IO;
using System.Threading.Tasks;

namespace DomainService.ServiceContract.CrossCutting;

public interface IFileStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
    Task<Stream> DownloadFileAsync(string fileId);
    Task DeleteFileAsync(string fileId);
}
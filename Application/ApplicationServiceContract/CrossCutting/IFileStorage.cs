using System.IO;
using System.Threading.Tasks;

namespace Application.ServiceContract.CrossCutting;

public interface IFileStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName);
    Task<Stream> DownloadFileAsync(string fileId);
    Task DeleteFileAsync(string fileId);
}
using Application.ServiceContract.CrossCutting;

namespace Infrastructure.InfrastructureShared.Services;

public class FileStorageService : IFileStorageService
{
    private readonly string _storagePath;

    public FileStorageService()
    {
        // 默认存储路径（实际应用中应从配置中读取）
        _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "FileStorage");

        // 确保存储目录存在
        if (!Directory.Exists(_storagePath))
            Directory.CreateDirectory(_storagePath);
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName)
    {
        if (fileStream == null)
            throw new ArgumentNullException(nameof(fileStream));

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("文件名不能为空", nameof(fileName));

        // 生成唯一文件ID
        var fileId = Guid.NewGuid().ToString();
        var fileExtension = Path.GetExtension(fileName);
        var storageFileName = $"{fileId}{fileExtension}";
        var filePath = Path.Combine(_storagePath, storageFileName);

        // 保存文件
        using (var file = File.Create(filePath))
        {
            await fileStream.CopyToAsync(file);
        }

        // 返回文件ID（可用于后续下载和删除）
        return fileId;
    }

    public async Task<Stream> DownloadFileAsync(string fileId)
    {
        if (string.IsNullOrWhiteSpace(fileId))
            throw new ArgumentException("文件ID不能为空", nameof(fileId));

        // 查找文件（简化实现：假设文件扩展名为.dat）
        var searchPattern = $"{fileId}.*";
        var files = Directory.GetFiles(_storagePath, searchPattern);

        if (files.Length == 0)
            throw new FileNotFoundException($"未找到ID为 {fileId} 的文件");

        var filePath = files[0];
        var fileStream = File.OpenRead(filePath);

        return fileStream;
    }

    public Task DeleteFileAsync(string fileId)
    {
        if (string.IsNullOrWhiteSpace(fileId))
            throw new ArgumentException("文件ID不能为空", nameof(fileId));

        // 查找文件
        var searchPattern = $"{fileId}.*";
        var files = Directory.GetFiles(_storagePath, searchPattern);

        if (files.Length == 0)
            throw new FileNotFoundException($"未找到ID为 {fileId} 的文件");

        var filePath = files[0];
        File.Delete(filePath);

        return Task.CompletedTask;
    }
}
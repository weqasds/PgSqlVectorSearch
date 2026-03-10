
namespace DomainService.ServiceContract.CrossCutting;

public interface ITaskQueueService
{
    Task<string> EnqueueDocumentProcessingAsync(Guid documentId);
    Task<string> EnqueueVectorGenerationAsync(Guid chunkId);
    Task<DomainService.Entities.Enums.TaskStatus> GetTaskStatusAsync(string taskId);
}
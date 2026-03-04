using System;
using System.Threading.Tasks;
using DomainService.Entities;

namespace DomainService.ServiceContract;

public interface ITaskQueueService
{
    Task<string> EnqueueDocumentProcessingAsync(Guid documentId);
    Task<string> EnqueueVectorGenerationAsync(Guid chunkId);
    Task<TaskStatus> GetTaskStatusAsync(string taskId);
}
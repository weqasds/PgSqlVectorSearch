using System;
using System.Threading.Tasks;
using DomainService.Entities.Core;
using DomainService.Entities.KnowledgeManagement;
using DomainService.Entities.KnowledgeGraph;
using DomainService.Entities.Enums;
using DomainService.Entities.VectorSearch;
using DomainService.Entities.Chat;
using DomainService.Entities.UserManagement;
using DomainService.Entities.System;
using DomainService.Entities.RAG;
using DomainService.Entities.Analytics;

namespace DomainService.ServiceContract.CrossCutting;

public interface ITaskQueueService
{
    Task<string> EnqueueDocumentProcessingAsync(Guid documentId);
    Task<string> EnqueueVectorGenerationAsync(Guid chunkId);
    Task<Entities.Enums.TaskStatus> GetTaskStatusAsync(string taskId);
}
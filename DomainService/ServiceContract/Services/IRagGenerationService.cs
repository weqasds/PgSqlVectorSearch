using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DomainService.Entities;

namespace DomainService.ServiceContract.Services;

public interface IRagGenerationService
{
    Task<RagResponse> GenerateResponseAsync(string query, Guid? sessionId = null);
    Task<RagResponse> GenerateResponseWithContextAsync(string query, IEnumerable<KnowledgeChunk> context);
    Task LogRetrievalAsync(Guid generationId, IEnumerable<RetrievalLog> logs);
}
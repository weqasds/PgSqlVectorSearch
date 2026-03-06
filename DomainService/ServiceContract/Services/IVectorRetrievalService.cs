using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DomainService.Entities;
using Pgvector;

namespace DomainService.ServiceContract.Services;

public interface IVectorRetrievalService
{
    Task<IEnumerable<RetrievalResult>> SearchSimilarChunksAsync(string query, int topK = 5);
    Task<Vector> GenerateEmbeddingAsync(string text);
    Task<double> CalculateSimilarityAsync(Vector v1, Vector v2);
}
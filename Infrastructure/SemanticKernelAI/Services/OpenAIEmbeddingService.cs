using Microsoft.SemanticKernel;
using Pgvector;
using DomainService.ServiceContract.CrossCutting;
using Microsoft.Extensions.AI;

namespace Infrastructure.SemanticKernelAI.Services;

/// <summary>
/// OpenAI嵌入生成服务
/// 使用OpenAI嵌入API生成文本向量
/// </summary>
public class OpenAIEmbeddingService : IEmbeddingService
{
    private readonly Kernel _kernel;
    private readonly string _embeddingModelName;
    private readonly int _embeddingDimension;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;

    public OpenAIEmbeddingService(Kernel kernel,
        IEmbeddingGenerator<string,Embedding<float>> embeddingGenerator,
        string embeddingModelName = "text-embedding-3-small", 
        int embeddingDimension = 1536)
    {
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _embeddingGenerator = embeddingGenerator;
        _embeddingModelName = embeddingModelName;
        _embeddingDimension = embeddingDimension;
    }

    public async Task<Vector> GenerateEmbeddingAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("文本不能为空", nameof(text));

        try
        {
            //ITextEmbeddingGenerationService
            // 使用Semantic Kernel生成嵌入
            var embedding = await _embeddingGenerator.GenerateAsync(text);

            // 转换为Pgvector.Vector
            var vectorValues = embedding.Vector.ToArray();
            return new Vector(vectorValues);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"生成嵌入向量失败: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<Vector>> GenerateEmbeddingsAsync(IEnumerable<string> texts)
    {
        if (texts == null)
            throw new ArgumentNullException(nameof(texts));

        var textList = texts.ToList();
        if (!textList.Any())
            return Enumerable.Empty<Vector>();

        try
        {
            var embeddings = await _embeddingGenerator.
                                GenerateAsync(textList);

            var vectors = new List<Vector>();
            foreach (var embedding in embeddings)
            {
                var vectorValues = embedding.Vector.ToArray();
                vectors.Add(new Vector(vectorValues));
            }

            return vectors;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"批量生成嵌入向量失败: {ex.Message}", ex);
        }
    }

    public string GetEmbeddingModelName()
    {
        return _embeddingModelName;
    }

    public int GetEmbeddingDimension()
    {
        return _embeddingDimension;
    }
}
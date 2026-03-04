namespace Application.ApplicationServiceContract;

public class CreateKnowledgeBaseRequest
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
}
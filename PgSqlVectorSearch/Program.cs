using Microsoft.EntityFrameworkCore;
using Application.QueryServices;
using DomainService.ServiceContract.CrossCutting;
using DomainService.ServiceContract.Repositories;
using DomainService.ServiceContract.Services;
using DomainService.Services.VectorRetrieval;
using EFCoreInfrastructure;
using EFCoreInfrastructure.QueryServices;
using EFCoreInfrastructure.Repository;
using Infrastructure.SemanticKernelAI.Services;

namespace PgSqlVectorSearch;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 配置数据库连接字符串
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                              "Host=localhost;Database=ragdb;Username=postgres;Password=password";

        // 配置应用服务
        // Add services to the container.

        // 1. DbContext注册
        builder.Services.AddDbContext<RagDbContext>(options =>
            options.UseNpgsql(connectionString));

        // 2. QueryService注册（CQRS查询服务）
        builder.Services.AddScoped(typeof(IQueryService<>), typeof(QueryServiceBase<>));
        builder.Services.AddScoped<IVectorSearchQueryService, VectorSearchQueryService>();
        // 注意：IKnowledgeBaseQueryService和IKnowledgeChunkQueryService的实现可能在其他项目中

        // 3. 仓储接口注册
        builder.Services.AddScoped(typeof(IRepository<>), typeof(RepositoryBase<>));

        // 4. 向量检索相关服务注册
        builder.Services.AddScoped<IVectorStore, EmbeddingRepository>();
        builder.Services.AddScoped<IEmbeddingService, OpenAIEmbeddingService>();
        builder.Services.AddScoped<IVectorRetrievalService, VectorRetrievalService>();

        // 5. 控制器和其他服务
        builder.Services.AddControllers();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Application.ApplicationService;
using Application.ApplicationServiceContract;
using Application.QueryServices;
using DomainService.ServiceContract.CrossCutting;
using DomainService.Services.RAG;
using DomainService.ServiceContract.Repositories;
using DomainService.ServiceContract.Services;
using DomainService.Services.Auth;
using InfrastructureShared.Services;
using DomainService.Services.VectorRetrieval;
using EFCoreInfrastructure;
using EFCoreInfrastructure.QueryServices;
using EFCoreInfrastructure.Repository;
using Infrastructure.SemanticKernelAI.Services;
using PgSqlVectorSearch.Middleware;

namespace PgSqlVectorSearch;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 配置数据库连接字符串
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                              "Host=localhost;Database=ragdb;Username=postgres;Password=@qq13015361197";

        // 配置JWT认证
        var jwtSettingsSection = builder.Configuration.GetSection("JwtSettings");
        builder.Services.Configure<JwtSettings>(jwtSettingsSection);
        var jwtSettings = jwtSettingsSection.Get<JwtSettings>() ?? new JwtSettings();

        // 配置应用服务
        // Add services to the container.

        // 1. DbContext注册
        builder.Services.AddDbContext<RagDbContext>(options =>
            options.UseNpgsql(connectionString));

        // 2. JWT认证配置
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = jwtSettings.ValidateIssuer,
                ValidateAudience = jwtSettings.ValidateAudience,
                ValidateLifetime = jwtSettings.ValidateLifetime,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                ClockSkew = TimeSpan.FromSeconds(jwtSettings.ClockSkewSeconds)
            };
        });

        // 3. QueryService注册（CQRS查询服务）
        builder.Services.AddScoped(typeof(IQueryService<>), typeof(QueryServiceBase<>));
        builder.Services.AddScoped<IVectorSearchQueryService, VectorSearchQueryService>();
        // 注意：IKnowledgeBaseQueryService和IKnowledgeChunkQueryService的实现可能在其他项目中

        // 3. 仓储接口注册
        builder.Services.AddScoped(typeof(IRepository<>), typeof(RepositoryBase<>));

        // 4. 认证相关服务注册
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ITokenService, JwtTokenService>();
        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
        builder.Services.AddScoped<IAuthService, AuthService>();

        // 5. 向量检索相关服务注册
        builder.Services.AddScoped<IVectorStore, EmbeddingRepository>();
        builder.Services.AddScoped<IEmbeddingService, OpenAIEmbeddingService>();
        builder.Services.AddScoped<IVectorRetrievalService, VectorRetrievalService>();

        // 6. 系统监控服务注册
        builder.Services.AddScoped<ISystemMetricsCollector, SystemMetricsCollector>();

        // 7. RAG仓储注册
        builder.Services.AddScoped<IRagGenerationRepository, RagGenerationRepository>();
        builder.Services.AddScoped<IRagRetrievalLogRepository, RagRetrievalLogRepository>();

        // 8. 应用服务注册
        builder.Services.AddScoped<ISystemAdminAppService, SystemAdminAppService>();
        builder.Services.AddScoped<IAuthAppService, AuthAppService>();
        builder.Services.AddScoped<IUserAppService, UserAppService>();
        builder.Services.AddScoped<IRagChatAppService, RagChatAppService>();

        // 9. 领域服务注册
        builder.Services.AddScoped<IRagGenerationService, RagGenerationService>();

        // 10. 授权策略
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole("Admin"));
            options.AddPolicy("AuthenticatedUser", policy =>
                policy.RequireAuthenticatedUser());
        });

        // 7. 控制器和其他服务
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
        app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}
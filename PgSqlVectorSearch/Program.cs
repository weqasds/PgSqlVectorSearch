using Application.Extensions;
using DomainService.Entities.Chat;
using DomainService.Entities.Enums;
using DomainService.Entities.KnowledgeManagement;
using DomainService.Extension;
using DomainService.ServiceContract.CrossCutting;
using EFCoreInfrastructure;
using EFCoreInfrastructure.Extensions;
using Infrastructure.SemanticKernelAI.Extensions;
using InfrastructureShared.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PgSqlVectorSearch.Middleware;
using Scalar.AspNetCore;
using System.Text;
namespace PgSqlVectorSearch;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 配置数据库连接字符串
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                              "Host=localhost;Database=rag_db;Username=postgres;Password=@qq13015361197";

        // 配置JWT认证
        var jwtSettingsSection = builder.Configuration.GetSection("JwtSettings");
        builder.Services.Configure<JwtSettings>(jwtSettingsSection);
        var jwtSettings = jwtSettingsSection.Get<JwtSettings>() ?? new JwtSettings();

        // 1. DbContext注册
        builder.Services.AddDbContext<RagDbContext>(options =>{
            options.UseNpgsql(connectionString, option =>
            {
                option.MapEnum<ChatRole>("chat_role");
                option.MapEnum<RelationType>("relation_type");
                option.MapEnum<EntityType>("entity_type");
                option.UseVector();
                option.MapEnum<DocumentStatus>("document_status");
            });
            
        });

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

        // 3. HttpContextAccessor注册(非中间管道代码获取http上下文需要)
        builder.Services.AddHttpContextAccessor();

        #region 基础设施层服务注册
        builder.Services.AddInfrastructureSharedServices();
        builder.Services.AddEFCoreInfrastructureServices();
        builder.Services.AddSemanticKernelAIServices();
        #endregion
        #region 领域层服务注册
        builder.Services.AddDomainServices();
        #endregion
        #region 应用层服务注册
        builder.Services.AddApplicationServices();
        #endregion

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
        app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
            
        }
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}
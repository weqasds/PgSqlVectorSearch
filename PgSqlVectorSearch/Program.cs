using Application.Extensions;
using DomainService.Entities.Chat;
using DomainService.Entities.Enums;
using DomainService.Entities.KnowledgeManagement;
using DomainService.Extension;
using DomainService.ServiceContract.CrossCutting;
using Infrastructure.EFCoreInfrastructure;
using Infrastructure.EFCoreInfrastructure.Extensions;
using Infrastructure.SemanticKernelAI.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using PgSqlVectorSearch.Middleware;
using Scalar.AspNetCore;
using System.Text;
namespace PgSqlVectorSearch;

using Infrastructure.EFCoreInfrastructure.Services;
using Infrastructure.InfrastructureShared.Extension;
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 配置数据库连接字符串
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                              "Host=localhost;Database=rag_db;Username=postgres;Password=@qq13015461197";

        // 配置JWT认证
        var jwtSettingsSection = builder.Configuration.GetSection("JwtSettings");
        builder.Services.Configure<JwtSettings>(jwtSettingsSection);
        var jwtSettings = jwtSettingsSection.Get<JwtSettings>() ?? new JwtSettings();

        // 1. DbContext注册
        //builder.Services.AddScoped<AuditSaveChangesInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, AuditSaveChangesInterceptor>();
        builder.Services.AddDbContext<RagDbContext>((serviceProvider, options) =>{
            options.UseNpgsql(connectionString, option =>
            {
                option.MapEnum<ChatRole>("chat_role");
                option.MapEnum<RelationType>("relation_type");
                option.MapEnum<EntityType>("entity_type");
                option.MapEnum<DocumentStatus>("document_status");
                option.MapEnum<FileType>("file_type");
                option.UseVector();

            });
            options.AddInterceptors(serviceProvider.GetServices<ISaveChangesInterceptor>());
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
        builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
        // 3. HttpContextAccessor注册(非中间管道代码获取http上下文需要)
        builder.Services.AddHttpContextAccessor();
        // builder.Logging.ClearProviders();
        // builder.Logging.AddConsole();
        // builder.Logging.AddDebug();
        // builder.Logging.AddEventSourceLogger();
        #region 基础设施层服务注册
            builder.Services.AddInfrastructureSharedServices(builder.Configuration);
            builder.Services.AddEFCoreInfrastructureServices();
            builder.Services.AddSemanticKernelAIServices(builder.Configuration);
            // 注册请求跟踪服务
            //builder.Services.AddRequestTracking();
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
        builder.Services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

                document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Name = "Authorization",
                    Description = "JWT Authorization header using Bearer scheme."
                };
                return Task.CompletedTask;
            });
            options.AddOperationTransformer((operation, context, cancellationToken) =>
            {
                operation.Security ??= new List<OpenApiSecurityRequirement>();

                operation.Security.Add(
                    new OpenApiSecurityRequirement
                    {
                        [
                            new OpenApiSecuritySchemeReference("Bearer")    
                        ] = new List<string>()
                    });

                return Task.CompletedTask;
            });
        });
        var app = builder.Build();
        // 使用请求跟踪中间件（必须在异常处理之前）
        app.UseMiddleware<RequestTrackingMiddleware>();
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
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Pgvector.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
namespace EFCoreInfrastructure;
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
public class RagDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<ChatSession> ChatSessions { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<KnowledgeBase> KnowledgeBases { get; set; }
    public DbSet<KnowledgeDocument> KnowledgeDocuments { get; set; }
    public DbSet<KnowledgeChunk> KnowledgeChunks { get; set; }
    public DbSet<Embedding> Embeddings { get; set; }
    public DbSet<RagGeneration> RagGenerations { get; set; }
    public DbSet<RagRetrievalLog> RagRetrievalLogs { get; set; }
    public DbSet<KnowledgeRelation> KnowledgeRelations { get; set; }
    public DbSet<UserKnowledgeBase> UserKnowledgeBases { get; set; }
    public DbSet<GraphNode> GraphNodes { get; set; }
    public DbSet<GraphEdge> GraphEdges { get; set; }
    /// <summary>
    /// Configures conventions for the model by customizing the provided configuration builder.
    /// </summary>
    /// <remarks>Override this method to add or modify conventions that affect model building. Call the base
    /// implementation to retain default conventions unless intentionally replacing them.</remarks>
    /// <param name="configurationBuilder">The builder used to configure model conventions. Cannot be null.</param>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
    }
    public RagDbContext(DbContextOptions<RagDbContext> options) : base(options)
    {
    }
    /// <summary>
    /// Configures the database context options for this context instance.
    /// </summary>
    /// <remarks>Override this method to configure the context for use with a specific database provider or to
    /// set other options. This method is called each time a new instance of the context is created.</remarks>
    /// <param name="optionsBuilder">A builder used to configure options for this context, such as the database provider and connection string.</param>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // 仅在未配置的情况下配置连接
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql(
                "Host=localhost;Port=5432;Database=rag_db;Username=postgres;Password=@qq13015461197",
                options => {
                options.MapEnum<ChatRole>("chat_role");
                options.MapEnum<RelationType>("relation_type");
                options.MapEnum<EntityType>("entity_type");
                options.UseVector();
                options.MigrationsAssembly("DataMigration");
                options.MapEnum<DocumentStatus>("document_status");
            });
        }
        base.OnConfiguring(optionsBuilder);
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.ToTable("chat_message");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Content).
                IsRequired();
            entity.HasOne(x => x.ChatSession)
            .WithMany(x => x.Messages)
            .HasForeignKey(x => x.ChatSessionId)
            .OnDelete(DeleteBehavior.Cascade);
            entity.Property(x=>x.Role).HasColumnType("chat_role");
            entity.HasIndex(x => x.ChatSessionId);
            entity.HasIndex(x => x.CreatedAt);
        });
        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.ToTable("chat_session");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ChatTitle).IsRequired();
            entity.HasOne(x => x.User)
                .WithMany(x => x.ChatSessions)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.CreatedAt);

            // 与KnowledgeBase的关系（一对多）
            entity.HasOne(x => x.KnowledgeBase)
                .WithMany(x => x.ChatSessions)
                .HasForeignKey(x => x.KnowledgeBaseId)
                .OnDelete(DeleteBehavior.SetNull);

            // ReferencedKnowledgeBaseIds字段配置
            entity.Property(x => x.ReferencedKnowledgeBaseIds)
                .IsRequired(false);

            entity.HasIndex(x => x.KnowledgeBaseId);
        });
        modelBuilder.Entity<Embedding>(entity =>
        {
            entity.ToTable("embedding");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Vector).
                HasColumnType("vector(1536)").
                IsRequired();
            entity.HasOne(x => x.Chunk)
                .WithMany()
                .HasForeignKey(x => x.ChunkId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.ChunkId).IsUnique();
        });
        modelBuilder.Entity<KnowledgeBase>(entity =>
        {
            entity.ToTable("knowledge_base");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(x => x.Name).IsUnique();
            entity.Property(x => x.Description).IsRequired();
            entity.Property(x => x.IsPublic).IsRequired().HasDefaultValue(false);

            // 与所有者的关系（可为空）
            entity.HasOne(x => x.Owner)
                .WithMany()
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.SetNull);

            // 与用户访问权限的关系
            entity.HasMany(x => x.UserAccesses)
                .WithOne(x => x.KnowledgeBase)
                .HasForeignKey(x => x.KnowledgeBaseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.OwnerId);
            entity.HasIndex(x => x.IsPublic);
        });
        modelBuilder.Entity<KnowledgeChunk>(entity =>
        {
            entity.ToTable("knowledge_chunk");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Content).IsRequired();
            entity.Property(x => x.ChunkIndex).IsRequired();
            entity.HasOne(x => x.Document)
                .WithMany()
                .HasForeignKey(x => x.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.DocumentId, x.ChunkIndex }).IsUnique();
            entity.HasIndex(x => x.DocumentId);
        });
        modelBuilder.Entity<KnowledgeDocument>(entity =>
        {
            entity.ToTable("knowledge_document");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.FileName)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(x => x.FilePath)
                .IsRequired()
                .HasMaxLength(1024);

            entity.Property(x => x.FileSize)
                .IsRequired();

            entity.Property(x => x.ContentType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Status)
                .HasColumnType("document_status")   // PostgreSQL enum
                .HasDefaultValue(DocumentStatus.Pending);

            entity.Property(x => x.ChunkCount)
                .HasDefaultValue(0);

            entity.Property(x => x.FileHash)
                .IsRequired(false)
                .HasMaxLength(64);

            entity.HasOne(x => x.KnowledgeBase)
                .WithMany()
                .HasForeignKey(x => x.KnowledgeBaseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.KnowledgeBaseId);
            entity.HasIndex(x => x.FileHash);
        });
        modelBuilder.Entity<RagGeneration>(entity =>
        {
            entity.ToTable("rag_generation");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Prompt).IsRequired();
            entity.Property(x => x.Answer).IsRequired();
            entity.HasOne(x => x.ChatSession)
                .WithMany()
                .HasForeignKey(x => x.ChatSessionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.ChatSessionId);
        });
        modelBuilder.Entity<RagRetrievalLog>(entity =>
        {
            entity.ToTable("rag_retrieval_log");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Score).IsRequired();
            entity.HasOne(x => x.RagGeneration)
                .WithMany()
                .HasForeignKey(x => x.RagGenerationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Chunk)
                .WithMany()
                .HasForeignKey(x => x.ChunkId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.RagGenerationId);
            entity.HasIndex(x => x.ChunkId);
        });
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("user");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserName).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Email).IsRequired();
            entity.Property(x => x.PasswordHash).IsRequired();

            // 与可访问知识库的关系
            entity.HasMany(x => x.AccessibleKnowledgeBases)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserKnowledgeBase实体配置（用户与知识库的多对多关系）
        modelBuilder.Entity<UserKnowledgeBase>(entity =>
        {
            entity.ToTable("user_knowledge_base");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.AccessLevel)
                .IsRequired()
                .HasConversion<int>();

            // 与User的关系
            entity.HasOne(x => x.User)
                .WithMany(x => x.AccessibleKnowledgeBases)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // 与KnowledgeBase的关系
            entity.HasOne(x => x.KnowledgeBase)
                .WithMany(x => x.UserAccesses)
                .HasForeignKey(x => x.KnowledgeBaseId)
                .OnDelete(DeleteBehavior.Cascade);

            // 复合唯一索引，确保每个用户对每个知识库只有一条权限记录
            entity.HasIndex(x => new { x.UserId, x.KnowledgeBaseId })
                .IsUnique();

            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.KnowledgeBaseId);
            entity.HasIndex(x => x.AccessLevel);
        });



        // KnowledgeRelation实体配置
        modelBuilder.Entity<KnowledgeRelation>(entity =>
        {
            entity.ToTable("knowledge_relation");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.RelationType)
                .HasColumnType("relation_type")
                .IsRequired();

            entity.Property(x => x.Strength)
                .IsRequired()
                .HasDefaultValue(1.0f);

            entity.Property(x => x.Description)
                .IsRequired(false);

            // 与KnowledgeChunk的关系（源）
            entity.HasOne(x => x.SourceChunk)
                .WithMany(x => x.SourceRelations)
                .HasForeignKey(x => x.SourceChunkId)
                .OnDelete(DeleteBehavior.Cascade);

            // 与KnowledgeChunk的关系（目标）
            entity.HasOne(x => x.TargetChunk)
                .WithMany(x => x.TargetRelations)
                .HasForeignKey(x => x.TargetChunkId)
                .OnDelete(DeleteBehavior.Cascade);

            // 与User的关系（创建者）
            entity.HasOne(x => x.Creator)
                .WithMany(x => x.CreatedRelations)
                .HasForeignKey(x => x.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // 索引
            entity.HasIndex(x => x.SourceChunkId);
            entity.HasIndex(x => x.TargetChunkId);
            entity.HasIndex(x => x.RelationType);
            entity.HasIndex(x => new { x.SourceChunkId, x.TargetChunkId, x.RelationType })
                .IsUnique();
            entity.HasIndex(x => x.CreatedBy);
        });

        // GraphNode实体配置
        modelBuilder.Entity<GraphNode>(entity =>
        {
            entity.ToTable("graph_node");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.EntityType)
                .HasColumnType("entity_type")
                .IsRequired();

            entity.Property(x => x.PositionX)
                .IsRequired()
                .HasDefaultValue(0f);

            entity.Property(x => x.PositionY)
                .IsRequired()
                .HasDefaultValue(0f);

            entity.Property(x => x.Label)
                .IsRequired(false)
                .HasMaxLength(200);

            entity.Property(x => x.Properties)
                .IsRequired(false);

            // 索引
            entity.HasIndex(x => new { x.EntityId, x.EntityType })
                .IsUnique();
            entity.HasIndex(x => x.EntityType);
        });

        // GraphEdge实体配置
        modelBuilder.Entity<GraphEdge>(entity =>
        {
            entity.ToTable("graph_edge");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Label)
                .IsRequired(false)
                .HasMaxLength(200);

            entity.Property(x => x.VisualProperties)
                .IsRequired(false);

            // 与GraphNode的关系（源）
            entity.HasOne(x => x.SourceNode)
                .WithMany()
                .HasForeignKey(x => x.SourceNodeId)
                .OnDelete(DeleteBehavior.Cascade);

            // 与GraphNode的关系（目标）
            entity.HasOne(x => x.TargetNode)
                .WithMany()
                .HasForeignKey(x => x.TargetNodeId)
                .OnDelete(DeleteBehavior.Cascade);

            // 与KnowledgeRelation的关系（可选）
            entity.HasOne(x => x.Relation)
                .WithMany()
                .HasForeignKey(x => x.RelationId)
                .OnDelete(DeleteBehavior.SetNull);

            // 索引
            entity.HasIndex(x => x.SourceNodeId);
            entity.HasIndex(x => x.TargetNodeId);
            entity.HasIndex(x => x.RelationId);
            entity.HasIndex(x => new { x.SourceNodeId, x.TargetNodeId })
                .IsUnique();
        });

        base.OnModelCreating(modelBuilder);
    }
    public override int SaveChanges()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<Entity<Guid>>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.ModifiedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModifiedAt = now;
            }
            else if (entry.State == EntityState.Deleted)
            {
                entry.Entity.IsDeleted = true;
                entry.Entity.ModifiedAt = now;
            }
        }
        return base.SaveChanges();
    }

}

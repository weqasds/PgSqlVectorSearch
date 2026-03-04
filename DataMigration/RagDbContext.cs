using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Pgvector.EntityFrameworkCore;
namespace DataMigration;
using DomainService.Entities;
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
    public RagDbContext() : base()
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
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=rag_db;Username=postgres;Password=@qq13015461197",
            options => {
            options.MapEnum<ChatRole>("chat_role");
            options.UseVector();
        });
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
            entity.Property(x => x.FileName).IsRequired();
            entity.HasOne(x => x.KnowledgeBase)
                .WithMany()
                .HasForeignKey(x => x.KnowledgeBaseId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.KnowledgeBaseId);
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

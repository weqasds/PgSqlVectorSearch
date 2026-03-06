using DomainService.Entities;
using EFCoreInfrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataMigration;

internal class MigrationDesignTimeFactory : IDesignTimeDbContextFactory<RagDbContext>
{
    public RagDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<RagDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=rag_db;Username=postgres;Password=@qq13015461197",
                options =>  {
                    options.MapEnum<ChatRole>("chat_role");
                    options.MapEnum<RelationType>("relation_type");
                    options.MapEnum<EntityType>("entity_type");
                    options.MapEnum<DocumentStatus>("document_status");
                    options.MigrationsAssembly("DataMigration");
                    options.UseVector();
                }
            ).Options;
        return new RagDbContext(options);
    }
}

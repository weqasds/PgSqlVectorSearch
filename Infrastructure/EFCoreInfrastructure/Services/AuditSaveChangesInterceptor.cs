// AuditSaveChangesInterceptor.cs
using Application.ApplicationServiceContract.CrossCutting;
using DomainService.Entities.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;
using EFCoreInfrastructure.QueryServices;
using Application.QueryServices;
using ApplicationServiceContract.CrossCutting;
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;

    public AuditSaveChangesInterceptor(
        ICurrentUserService currentUserService
        )
    {
        _currentUserService = currentUserService;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        AuditChanges(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AuditChanges(eventData.Context);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AuditChanges(DbContext? context)
    {
        if (context == null) return;
        Console.WriteLine(context.GetHashCode());
        var changeTracker = context.ChangeTracker;
        var auditEntries = new List<AuditLog>();
        var userId = _currentUserService.GetCurrentUserId();

        foreach (var entry in changeTracker.Entries())
        {
            // 跳过审计日志实体自身，避免递归
            if (entry.Entity is AuditLog) continue;

            // 跳过未更改的实体
            if (entry.State == EntityState.Unchanged ||
                entry.State == EntityState.Detached) continue;

            var tableName = entry.Metadata.GetTableName() ?? entry.Metadata.ClrType.Name;
            var primaryKey = GetPrimaryKeyValue(entry);
            //查询修改的属性
            var modifiedProperties = entry.State switch
            {
                EntityState.Added => [.. entry.Properties.Where(p => !p.IsTemporary)],
                EntityState.Modified => [.. entry.Properties.Where(p => p.IsModified && !p.IsTemporary)],
                EntityState.Deleted => [.. entry.Properties.Where(p => !p.IsTemporary)],
                _ => entry.Properties.Where(p => !p.IsTemporary).ToList()
            };
            var oldValues = entry.State != EntityState.Added ? modifiedProperties.ToDictionary(
                            k => k.Metadata.Name, k => k.OriginalValue
                        ) : null;
            var newValues = entry.State != EntityState.Deleted ? modifiedProperties.ToDictionary(
                        p => p.Metadata.Name,
                        p => p.CurrentValue
                    ) : null;
            switch (entry.State)
            {
                case EntityState.Added:


                    var addedLog = AuditLog.Create(
                        operation: "CREATE",
                        tableName: tableName,
                        entityId: primaryKey,
                        entityName: entry.Entity.GetType().Name,
                        userId: userId,
                        newValues: JsonSerializer.SerializeToElement(newValues),
                        affectedColumns: string.Join(",", newValues.Keys),
                        details: string.Join(";", modifiedProperties.Select(p =>$"{p.Metadata.Name}:{p.CurrentValue}"))
                    );
                    auditEntries.Add(addedLog);
                    break;
                case EntityState.Modified:
                    if (!modifiedProperties.Any()) break;
                    var log = AuditLog.Create(
                        "UPDATE",
                        tableName: tableName,
                        entityId: primaryKey,
                        entityName: entry.Entity.GetType().Name,
                        userId: userId,
                        oldValues: JsonSerializer.SerializeToElement(oldValues),
                        newValues: JsonSerializer.SerializeToElement(newValues),
                        affectedColumns: string.Join(",", newValues.Keys)
                    );

                    log.AppendDetails(
                        string.Join("; ",
                            //changeValues.Select(kv =>
                            //    $"{kv.Key}: {kv.Value.Old} -> {kv.Value.New}"))
                            modifiedProperties.Select(p => $"{p.Metadata.Name}: {p.OriginalValue} -> {p.CurrentValue}")
                            )
                        );
                    auditEntries.Add(log);
                    break;

                case EntityState.Deleted:
                    var deletedLog = AuditLog.Create(
                        operation: "DELETE",
                        userId: userId,
                        tableName: tableName,
                        entityId: primaryKey,
                        entityName: entry.Entity.GetType().Name,
                        oldValues: JsonSerializer.SerializeToElement(oldValues),
                        affectedColumns: string.Join(",", oldValues.Keys)
                    );
                    auditEntries.Add(deletedLog);
                    break;
            }
        }

        // 异步保存审计日志（不阻塞主流程）
        if (auditEntries.Count > 0)
        {
            context.Set<AuditLog>().AddRange(auditEntries);
        }
    }

    private static string? GetPrimaryKeyValue(EntityEntry entry)
    {
        var primaryKey = entry.Metadata.FindPrimaryKey();
        if (primaryKey == null) return null;

        var values = primaryKey.Properties
            .Select(p => entry.Property(p.Name).CurrentValue?.ToString())
            .Where(v => !string.IsNullOrEmpty(v))
            .ToList();

        return values.Any() ? string.Join(",", values) : "未知";
    }
}
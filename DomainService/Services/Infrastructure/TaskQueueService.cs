using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using DomainService.Entities;
using DomainService.ServiceContract.Services;
using DomainService.ServiceContract.Repositories;
using DomainService.ServiceContract.CrossCutting;

namespace DomainService.Services.Infrastructure;

public class TaskQueueService : ITaskQueueService
{
    private readonly ConcurrentDictionary<string, TaskItem> _tasks = new();

    public Task<string> EnqueueDocumentProcessingAsync(Guid documentId)
    {
        var taskId = Guid.NewGuid().ToString();
        var taskItem = new TaskItem
        {
            TaskId = taskId,
            Type = TaskType.DocumentProcessing,
            TargetId = documentId,
            Status = Entities.TaskStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _tasks[taskId] = taskItem;

        // TODO: 实际队列实现（如使用RabbitMQ、Azure Queue等）
        // 这里只是模拟入队操作

        return Task.FromResult(taskId);
    }

    public Task<string> EnqueueVectorGenerationAsync(Guid chunkId)
    {
        var taskId = Guid.NewGuid().ToString();
        var taskItem = new TaskItem
        {
            TaskId = taskId,
            Type = TaskType.VectorGeneration,
            TargetId = chunkId,
            Status = Entities.TaskStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _tasks[taskId] = taskItem;

        // TODO: 实际队列实现
        // 这里只是模拟入队操作

        return Task.FromResult(taskId);
    }

    public Task<Entities.TaskStatus> GetTaskStatusAsync(string taskId)
    {
        if (string.IsNullOrWhiteSpace(taskId))
            throw new ArgumentException("任务ID不能为空", nameof(taskId));

        if (_tasks.TryGetValue(taskId, out var taskItem))
        {
            // 模拟任务状态变化（实际应用中应有后台工作者更新状态）
            if (taskItem.Status == Entities.TaskStatus.Pending &&
                (DateTime.UtcNow - taskItem.CreatedAt).TotalSeconds > 5)
            {
                taskItem.Status = Entities.TaskStatus.Completed;
                taskItem.UpdatedAt = DateTime.UtcNow;
            }

            return Task.FromResult(taskItem.Status);
        }

        throw new KeyNotFoundException($"未找到ID为 {taskId} 的任务");
    }

    private class TaskItem
    {
        public string TaskId { get; set; } = null!;
        public TaskType Type { get; set; }
        public Guid TargetId { get; set; }
        public Entities.TaskStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    private enum TaskType
    {
        DocumentProcessing,
        VectorGeneration
    }
}
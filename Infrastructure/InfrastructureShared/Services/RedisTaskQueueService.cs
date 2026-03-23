using System;
using System.Text.Json;
using System.Threading.Tasks;
using DomainService.Entities.Enums;
using DomainService.ServiceContract.CrossCutting;
using StackExchange.Redis;

namespace Infrastructure.InfrastructureShared.Services;

/// <summary>
/// Redis任务队列服务实现
/// 使用Redis作为消息队列存储任务
/// </summary>
public class RedisTaskQueueService : ITaskQueueService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private const string TaskQueueKey = "task:queue";
    private const string TaskDetailKeyPrefix = "task:detail:";
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisTaskQueueService(IConnectionMultiplexer redis)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _database = redis.GetDatabase();

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<string> EnqueueDocumentProcessingAsync(Guid documentId)
    {
        var taskId = Guid.NewGuid().ToString();
        var taskItem = new RedisTaskItem
        {
            TaskId = taskId,
            Type = TaskType.DocumentProcessing,
            TargetId = documentId,
            Status = DomainService.Entities.Enums.TaskStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 序列化任务并存储到Redis
        var taskJson = JsonSerializer.Serialize(taskItem, _jsonOptions);

        // 1. 将任务ID添加到队列
        await _database.ListRightPushAsync(TaskQueueKey, taskId);

        // 2. 存储任务详情到Hash
        var taskDetailKey = GetTaskDetailKey(taskId);
        var hashEntries = new[]
        {
            new HashEntry("taskId", taskItem.TaskId),
            new HashEntry("type", taskItem.Type.ToString()),
            new HashEntry("targetId", taskItem.TargetId.ToString()),
            new HashEntry("status", taskItem.Status.ToString()),
            new HashEntry("createdAt", taskItem.CreatedAt.ToString("o")),
            new HashEntry("updatedAt", taskItem.UpdatedAt.ToString("o")),
            new HashEntry("fullJson", taskJson) // 存储完整JSON以便反序列化
        };

        await _database.HashSetAsync(taskDetailKey, hashEntries);

        return taskId;
    }

    public async Task<string> EnqueueVectorGenerationAsync(Guid chunkId)
    {
        var taskId = Guid.NewGuid().ToString();
        var taskItem = new RedisTaskItem
        {
            TaskId = taskId,
            Type = TaskType.VectorGeneration,
            TargetId = chunkId,
            Status = DomainService.Entities.Enums.TaskStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 序列化任务并存储到Redis
        var taskJson = JsonSerializer.Serialize(taskItem, _jsonOptions);

        // 1. 将任务ID添加到队列
        await _database.ListRightPushAsync(TaskQueueKey, taskId);

        // 2. 存储任务详情到Hash
        var taskDetailKey = GetTaskDetailKey(taskId);
        var hashEntries = new[]
        {
            new HashEntry("taskId", taskItem.TaskId),
            new HashEntry("type", taskItem.Type.ToString()),
            new HashEntry("targetId", taskItem.TargetId.ToString()),
            new HashEntry("status", taskItem.Status.ToString()),
            new HashEntry("createdAt", taskItem.CreatedAt.ToString("o")),
            new HashEntry("updatedAt", taskItem.UpdatedAt.ToString("o")),
            new HashEntry("fullJson", taskJson) // 存储完整JSON以便反序列化
        };

        await _database.HashSetAsync(taskDetailKey, hashEntries);

        return taskId;
    }

    public async Task<DomainService.Entities.Enums.TaskStatus> GetTaskStatusAsync(string taskId)
    {
        if (string.IsNullOrWhiteSpace(taskId))
            throw new ArgumentException("任务ID不能为空", nameof(taskId));

        var taskDetailKey = GetTaskDetailKey(taskId);

        // 从Redis获取任务状态
        var statusValue = await _database.HashGetAsync(taskDetailKey, "status");

        if (statusValue.HasValue)
        {
            if (Enum.TryParse<DomainService.Entities.Enums.TaskStatus>(statusValue, out var status))
            {
                return status;
            }
        }

        throw new KeyNotFoundException($"未找到ID为 {taskId} 的任务");
    }

    /// <summary>
    /// 获取任务详情（内部方法，可用于后台工作者）
    /// </summary>
    public async Task<RedisTaskItem?> GetTaskAsync(string taskId)
    {
        if (string.IsNullOrWhiteSpace(taskId))
            return null;

        var taskDetailKey = GetTaskDetailKey(taskId);
        var fullJson = await _database.HashGetAsync(taskDetailKey, "fullJson");

        if (fullJson.HasValue)
        {
            var json = (string?)fullJson;
            return !string.IsNullOrEmpty(json)? JsonSerializer.Deserialize<RedisTaskItem>(json, _jsonOptions): 
                null;
        }
        return null;
    }

    /// <summary>
    /// 更新任务状态（内部方法，可用于后台工作者）
    /// </summary>
    public async Task UpdateTaskStatusAsync(string taskId, DomainService.Entities.Enums.TaskStatus status)
    {
        var taskDetailKey = GetTaskDetailKey(taskId);

        // 更新状态
        await _database.HashSetAsync(taskDetailKey, "status", status.ToString());
        await _database.HashSetAsync(taskDetailKey, "updatedAt", DateTime.UtcNow.ToString("o"));

        // 同时更新完整JSON中的状态
        var fullJson = await _database.HashGetAsync(taskDetailKey, "fullJson");
        if (fullJson.HasValue)
        {
            var json = (string?)fullJson;

            var taskItem = !string.IsNullOrEmpty(json)?JsonSerializer.Deserialize<RedisTaskItem>(json, _jsonOptions):
                null;
            if (taskItem != null)
            {
                taskItem.Status = status;
                taskItem.UpdatedAt = DateTime.UtcNow;
                var updatedJson = JsonSerializer.Serialize(taskItem, _jsonOptions);
                await _database.HashSetAsync(taskDetailKey, "fullJson", updatedJson);
            }
        }
    }

    /// <summary>
    /// 从队列中获取下一个任务（内部方法，可用于后台工作者）
    /// </summary>
    public async Task<string?> DequeueTaskAsync()
    {
        // 从队列左侧弹出任务ID
        var taskId = await _database.ListLeftPopAsync(TaskQueueKey);
        return taskId;
    }

    private static string GetTaskDetailKey(string taskId)
    {
        return $"{TaskDetailKeyPrefix}{taskId}";
    }

    /// <summary>
    /// Redis任务项
    /// </summary>
    public class RedisTaskItem
    {
        public string TaskId { get; set; } = null!;
        public TaskType Type { get; set; }
        public Guid TargetId { get; set; }
        public DomainService.Entities.Enums.TaskStatus Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

    public enum TaskType
    {
        DocumentProcessing,
        VectorGeneration
    }
}
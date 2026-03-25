# C# 调用示例

## 基本用法

```csharp
var httpClient = new HttpClient
{
    BaseAddress = new Uri("http://localhost:8000")
};

var client = new DocumentParserClient(httpClient, bearerToken: null);
var parseResponse = await client.ParseAsync(@"C:\docs\sample.pdf");
Console.WriteLine(parseResponse?.Data?.DocumentMetadata?.Title);

var taskResponse = await client.SubmitAsync(@"C:\docs\large.pdf");
var taskId = taskResponse?.Data?.TaskId;
var status = await client.GetTaskStatusAsync(taskId!);
var result = await client.GetTaskResultAsync(taskId!);
```

## 对接说明

- 同步解析接口：`POST /parse`
- 异步提交接口：`POST /parse/async`
- 任务状态接口：`GET /tasks/{task_id}`
- 任务结果接口：`GET /tasks/{task_id}/result`
- 健康检查接口：`GET /health` 和 `GET /ready`

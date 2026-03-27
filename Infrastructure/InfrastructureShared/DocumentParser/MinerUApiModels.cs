using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.InfrastructureShared.DocumentParser;

#region 通用 API 响应模型

/// <summary>
/// MinerU API 通用响应包装器。
/// </summary>
public sealed class MinerUApiResponse<T>
{
    /// <summary>
    /// 响应代码：0 表示成功，非 0 表示错误。
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// 响应消息。
    /// </summary>
    [JsonPropertyName("msg")]
    public string Msg { get; set; } = string.Empty;

    /// <summary>
    /// 追踪 ID。
    /// </summary>
    [JsonPropertyName("trace_id")]
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// 响应数据。
    /// </summary>
    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

#endregion

#region 批量文件上传 API 模型

/// <summary>
/// 批量文件上传请求项。
/// </summary>
public sealed class MinerUBatchFileRequestItem
{
    /// <summary>
    /// 文件名（含扩展名）。
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 用户自定义数据 ID，用于关联业务数据。
    /// </summary>
    [JsonPropertyName("data_id")]
    public string? DataId { get; set; }
}

/// <summary>
/// 批量文件上传请求。
/// </summary>
public sealed class MinerUBatchFileRequest
{
    /// <summary>
    /// 文件列表。
    /// </summary>
    [JsonPropertyName("files")]
    public List<MinerUBatchFileRequestItem> Files { get; set; } = new();
}

/// <summary>
/// 批量文件上传响应数据。
/// </summary>
public sealed class MinerUBatchFileResponseData
{
    /// <summary>
    /// 批次 ID。
    /// </summary>
    [JsonPropertyName("batch_id")]
    public string BatchId { get; set; } = string.Empty;

    /// <summary>
    /// 预签名的上传 URL 列表（字符串数组）。
    /// </summary>
    [JsonPropertyName("file_urls")]
    public List<string> FileUrls { get; set; } = new();
}

#endregion

#region 批量提取结果 API 模型

/// <summary>
/// 提取进度信息。
/// </summary>
public sealed class MinerUExtractProgress
{
    /// <summary>
    /// 已处理页数。
    /// </summary>
    [JsonPropertyName("page_processed")]
    public int PageProcessed { get; set; }

    /// <summary>
    /// 总页数。
    /// </summary>
    [JsonPropertyName("page_total")]
    public int PageTotal { get; set; }
}

/// <summary>
/// 单个文件的提取结果。
/// </summary>
public sealed class MinerUExtractResultItem
{
    /// <summary>
    /// 任务状态："running"、"done"、"failed"。
    /// </summary>
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// 提取进度。
    /// </summary>
    [JsonPropertyName("extract_progress")]
    public MinerUExtractProgress? ExtractProgress { get; set; }

    /// <summary>
    /// 错误信息（如果有）。
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// 结果 ZIP 包下载 URL（仅当 state 为 "done" 时存在）。
    /// </summary>
    [JsonPropertyName("full_zip_url")]
    public string? FullZipUrl { get; set; }
}

/// <summary>
/// 批量提取结果响应数据。
/// </summary>
public sealed class MinerUExtractResultBatchResponseData
{
    /// <summary>
    /// 批次 ID。
    /// </summary>
    [JsonPropertyName("batch_id")]
    public string BatchId { get; set; } = string.Empty;

    /// <summary>
    /// 提取结果列表。
    /// </summary>
    [JsonPropertyName("extract_result")]
    public List<MinerUExtractResultItem> ExtractResult { get; set; } = new();
}

#endregion

#region content_list.json 模型

/// <summary>
/// 归一化边界框（0‑1000 坐标系）。
/// </summary>
public sealed class MinerUBoundingBox
{
    /// <summary>
    /// 左上角 X 坐标。
    /// </summary>
    [JsonPropertyName("x0")]
    public double X0 { get; set; }

    /// <summary>
    /// 左上角 Y 坐标。
    /// </summary>
    [JsonPropertyName("y0")]
    public double Y0 { get; set; }

    /// <summary>
    /// 右下角 X 坐标。
    /// </summary>
    [JsonPropertyName("x1")]
    public double X1 { get; set; }

    /// <summary>
    /// 右下角 Y 坐标。
    /// </summary>
    [JsonPropertyName("y1")]
    public double Y1 { get; set; }
}

/// <summary>
/// 内容块基类。
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(MinerUTextBlock), typeDiscriminator: "text")]
[JsonDerivedType(typeof(MinerUImageBlock), typeDiscriminator: "image")]
[JsonDerivedType(typeof(MinerUTableBlock), typeDiscriminator: "table")]
[JsonDerivedType(typeof(MinerUEquationBlock), typeDiscriminator: "equation")]
[JsonDerivedType(typeof(MinerUCodeBlock), typeDiscriminator: "code")]
[JsonDerivedType(typeof(MinerUListBlock), typeDiscriminator: "list")]
[JsonDerivedType(typeof(MinerUDiscardedBlock), typeDiscriminator: "discarded")]
public abstract class MinerUContentBlock
{
    /// <summary>
    /// 块类型："text"、"image"、"table"、"equation"、"code"、"list" 等。
    /// </summary>
    [JsonIgnore]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 归一化边界框。
    /// </summary>
    [JsonPropertyName("bbox")]
    public MinerUBoundingBox? Bbox { get; set; }

    /// <summary>
    /// 页码（从 0 开始）。
    /// </summary>
    [JsonPropertyName("page_idx")]
    public int PageIdx { get; set; }
}

/// <summary>
/// 文本块（正文或标题）。
/// </summary>
public sealed class MinerUTextBlock : MinerUContentBlock
{
    /// <summary>
    /// 文本内容。
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 文本层级：0=正文，1=一级标题，2=二级标题，依此类推。
    /// </summary>
    [JsonPropertyName("text_level")]
    public int TextLevel { get; set; }
}

/// <summary>
/// 图片块。
/// </summary>
public sealed class MinerUImageBlock : MinerUContentBlock
{
    /// <summary>
    /// 图片文件路径（相对于 ZIP 根目录）。
    /// </summary>
    [JsonPropertyName("img_path")]
    public string ImgPath { get; set; } = string.Empty;

    /// <summary>
    /// 图片标题列表。
    /// </summary>
    [JsonPropertyName("image_caption")]
    public List<string> ImageCaption { get; set; } = new();

    /// <summary>
    /// 图片脚注列表。
    /// </summary>
    [JsonPropertyName("image_footnote")]
    public List<string> ImageFootnote { get; set; } = new();
}

/// <summary>
/// 表格块。
/// </summary>
public sealed class MinerUTableBlock : MinerUContentBlock
{
    /// <summary>
    /// 表格图片路径。
    /// </summary>
    [JsonPropertyName("img_path")]
    public string ImgPath { get; set; } = string.Empty;

    /// <summary>
    /// 表格标题列表。
    /// </summary>
    [JsonPropertyName("table_caption")]
    public List<string> TableCaption { get; set; } = new();

    /// <summary>
    /// 表格脚注列表。
    /// </summary>
    [JsonPropertyName("table_footnote")]
    public List<string> TableFootnote { get; set; } = new();

    /// <summary>
    /// 表格内容 HTML 字符串。
    /// </summary>
    [JsonPropertyName("table_body")]
    public string TableBody { get; set; } = string.Empty;
}

/// <summary>
/// 公式块。
/// </summary>
public sealed class MinerUEquationBlock : MinerUContentBlock
{
    /// <summary>
    /// 公式图片路径。
    /// </summary>
    [JsonPropertyName("img_path")]
    public string ImgPath { get; set; } = string.Empty;

    /// <summary>
    /// LaTeX 公式字符串。
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 文本格式（固定为 "latex"）。
    /// </summary>
    [JsonPropertyName("text_format")]
    public string TextFormat { get; set; } = "latex";
}

/// <summary>
/// 代码块。
/// </summary>
public sealed class MinerUCodeBlock : MinerUContentBlock
{
    /// <summary>
    /// 子类型："code" 或 "algorithm"。
    /// </summary>
    [JsonPropertyName("sub_type")]
    public string SubType { get; set; } = string.Empty;

    /// <summary>
    /// 代码内容。
    /// </summary>
    [JsonPropertyName("code_body")]
    public string CodeBody { get; set; } = string.Empty;

    /// <summary>
    /// 代码标题列表。
    /// </summary>
    [JsonPropertyName("code_caption")]
    public List<string> CodeCaption { get; set; } = new();
}

/// <summary>
/// 列表块。
/// </summary>
public sealed class MinerUListBlock : MinerUContentBlock
{
    /// <summary>
    /// 子类型："text" 或 "ref_text"。
    /// </summary>
    [JsonPropertyName("sub_type")]
    public string SubType { get; set; } = string.Empty;

    /// <summary>
    /// 列表项数组。
    /// </summary>
    [JsonPropertyName("list_items")]
    public List<string> ListItems { get; set; } = new();
}

/// <summary>
/// 废弃块（页眉、页脚、页码、侧边文本、页脚文本等）。
/// </summary>
public sealed class MinerUDiscardedBlock : MinerUContentBlock
{
    /// <summary>
    /// 文本内容。
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}


#endregion

#region JSON 转换器

/// <summary>
/// 将边界框数组 [x0, y0, x1, y1] 转换为 MinerUBoundingBox 对象的 JSON 转换器。
/// 支持数组格式和对象格式。
/// </summary>
public sealed class MinerUBoundingBoxConverter : JsonConverter<MinerUBoundingBox>
{
    public override MinerUBoundingBox Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            // 数组格式: [x0, y0, x1, y1]
            var array = JsonSerializer.Deserialize<double[]>(ref reader, options)
                ?? throw new JsonException("无法解析边界框数组。");

            if (array.Length != 4)
            {
                throw new JsonException($"边界框数组必须包含 4 个元素，实际有 {array.Length} 个。");
            }

            return new MinerUBoundingBox
            {
                X0 = array[0],
                Y0 = array[1],
                X1 = array[2],
                Y1 = array[3]
            };
        }
        else if (reader.TokenType == JsonTokenType.StartObject)
        {
            // 对象格式: { "x0": ..., "y0": ..., "x1": ..., "y1": ... }
            return JsonSerializer.Deserialize<MinerUBoundingBox>(ref reader, options)
                ?? throw new JsonException("无法解析边界框对象。");
        }
        else
        {
            throw new JsonException($"无法解析边界框，期待数组或对象，实际是 {reader.TokenType}。");
        }
    }

    public override void Write(
        Utf8JsonWriter writer,
        MinerUBoundingBox value,
        JsonSerializerOptions options)
    {
        // 序列化为数组格式以保持与 MinerU 输出的兼容性
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X0);
        writer.WriteNumberValue(value.Y0);
        writer.WriteNumberValue(value.X1);
        writer.WriteNumberValue(value.Y1);
        writer.WriteEndArray();
    }
}

#endregion
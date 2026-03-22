using System.IO;

namespace Infrastructure.InfrastructureShared.DocumentParser.Helpers;

/// <summary>
/// 文档加载策略决策器
/// </summary>
internal static class DocumentLoadStrategy
{
    /// <summary>
    /// 小文件阈值（字节数），小于此值可使用 DOM 加载
    /// </summary>
    private const long SmallFileThreshold = 10 * 1024 * 1024; // 10MB

    /// <summary>
    /// 大文件阈值（字节数），大于此值必须使用流式加载
    /// </summary>
    private const long LargeFileThreshold = 100 * 1024 * 1024; // 100MB

    /// <summary>
    /// 判断是否应该使用流式加载
    /// </summary>
    public static bool ShouldUseStreaming(Stream stream)
    {
        if (!stream.CanSeek)
            return true; // 无法获取长度，使用流式加载

        var length = stream.Length;

        // 超大文件必须使用流式
        if (length > LargeFileThreshold)
            return true;

        // 小文件可以使用 DOM 加载
        if (length < SmallFileThreshold)
            return false;

        // 中等文件默认使用流式，但可根据具体情况调整
        return true;
    }

    /// <summary>
    /// 判断是否应该使用并行处理
    /// </summary>
    public static bool ShouldUseParallelProcessing(Stream stream)
    {
        if (!stream.CanSeek)
            return false; // 无法获取长度，不使用并行

        var length = stream.Length;

        // 大文件使用并行处理
        return length > SmallFileThreshold;
    }

    /// <summary>
    /// 获取适合的并行度
    /// </summary>
    public static int GetOptimalParallelism(Stream stream)
    {
        if (!ShouldUseParallelProcessing(stream))
            return 1;

        // 基于文件大小和处理器数量计算
        var processorCount = Environment.ProcessorCount;
        if (!stream.CanSeek)
            return Math.Max(1, processorCount / 2);

        var length = stream.Length;

        // 每 50MB 分配一个处理器，但不超过总处理器数
        var basedOnSize = (int)Math.Min(length / (50 * 1024 * 1024) + 1, processorCount);
        return Math.Max(1, Math.Min(basedOnSize, processorCount));
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Infrastructure.InfrastructureShared.DocumentParser.Helpers;

/// <summary>
/// XML 流式解析辅助类
/// </summary>
internal static class XmlParserHelper
{
    /// <summary>
    /// 使用流式方式解析 XML 文档
    /// </summary>
    public static async Task<IEnumerable<XmlElementInfo>> ParseStreamingAsync(
        Stream stream,
        string rootElementName,
        string targetElementName,
        CancellationToken cancellationToken)
    {
        var elements = new List<XmlElementInfo>();

        using var reader = XmlReader.Create(stream, new XmlReaderSettings
        {
            Async = true,
            IgnoreWhitespace = true,
            IgnoreComments = true
        });

        // 状态机：跟踪当前元素路径
        var elementStack = new Stack<string>();
        var currentElement = new StringBuilder();
        bool insideTargetElement = false;
        int depth = 0;

        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    elementStack.Push(reader.Name);
                    depth++;

                    if (reader.Name == targetElementName && elementStack.Contains(rootElementName))
                    {
                        insideTargetElement = true;
                        currentElement.Clear();
                    }

                    // 读取元素属性（如果需要）
                    if (reader.HasAttributes)
                    {
                        // 可以在这里处理属性
                    }

                    break;

                case XmlNodeType.Text:
                case XmlNodeType.CDATA:
                    if (insideTargetElement)
                    {
                        currentElement.Append(reader.Value);
                    }
                    break;

                case XmlNodeType.EndElement:
                    if (insideTargetElement && reader.Name == targetElementName)
                    {
                        elements.Add(new XmlElementInfo
                        {
                            Path = string.Join("/", elementStack.Reverse()),
                            Content = currentElement.ToString().Trim(),
                            Depth = depth
                        });
                        insideTargetElement = false;
                    }

                    elementStack.Pop();
                    depth--;
                    break;
            }
        }

        return elements;
    }

    /// <summary>
    /// 使用状态机模式解析表格数据
    /// </summary>
    public static async IAsyncEnumerable<TableRow> ParseTableAsync(
        Stream stream,
        string rowElementName,
        string cellElementName,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var reader = XmlReader.Create(stream, new XmlReaderSettings
        {
            Async = true,
            IgnoreWhitespace = true,
            IgnoreComments = true
        });

        var currentRow = new List<string>();
        bool insideRow = false;
        bool insideCell = false;
        var currentCell = new StringBuilder();

        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    if (reader.Name == rowElementName)
                    {
                        insideRow = true;
                        currentRow.Clear();
                    }
                    else if (insideRow && reader.Name == cellElementName)
                    {
                        insideCell = true;
                        currentCell.Clear();
                    }
                    break;

                case XmlNodeType.Text:
                case XmlNodeType.CDATA:
                    if (insideCell)
                    {
                        currentCell.Append(reader.Value);
                    }
                    break;

                case XmlNodeType.EndElement:
                    if (reader.Name == cellElementName && insideCell)
                    {
                        currentRow.Add(currentCell.ToString().Trim());
                        insideCell = false;
                    }
                    else if (reader.Name == rowElementName && insideRow)
                    {
                        yield return new TableRow { Cells = currentRow.ToArray() };
                        insideRow = false;
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// 快速检查 XML 文档大小和结构
    /// </summary>
    public static async Task<XmlDocumentInfo> AnalyzeDocumentAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        using var reader = XmlReader.Create(stream, new XmlReaderSettings
        {
            Async = true,
            IgnoreWhitespace = true,
            IgnoreComments = true
        });

        var elementCounts = new Dictionary<string, int>();
        var maxDepth = 0;
        var currentDepth = 0;
        bool hasComplexStructure = false;

        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    currentDepth++;
                    maxDepth = Math.Max(maxDepth, currentDepth);

                    var name = reader.Name;
                    if (elementCounts.ContainsKey(name))
                        elementCounts[name]++;
                    else
                        elementCounts[name] = 1;

                    // 检查是否有子元素
                    if (!reader.IsEmptyElement)
                    {
                        hasComplexStructure = true;
                    }
                    break;

                case XmlNodeType.EndElement:
                    currentDepth--;
                    break;
            }
        }

        return new XmlDocumentInfo
        {
            ElementCounts = elementCounts,
            MaxDepth = maxDepth,
            HasComplexStructure = hasComplexStructure,
            TotalElements = elementCounts.Values.Sum()
        };
    }

    /// <summary>
    /// 剪枝解析：只读取特定路径的元素
    /// </summary>
    public static async IAsyncEnumerable<XmlElementInfo> ParseWithPruningAsync(
        Stream stream,
        HashSet<string> targetPaths,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var reader = XmlReader.Create(stream, new XmlReaderSettings
        {
            Async = true,
            IgnoreWhitespace = true,
            IgnoreComments = true
        });

        var elementStack = new Stack<string>();
        var currentPath = new List<string>();
        var currentElement = new StringBuilder();
        bool insideTargetElement = false;
        string? currentTargetPath = null;

        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    elementStack.Push(reader.Name);
                    currentPath.Add(reader.Name);
                    var path = string.Join("/", currentPath);

                    if (targetPaths.Contains(path))
                    {
                        insideTargetElement = true;
                        currentTargetPath = path;
                        currentElement.Clear();
                    }
                    break;

                case XmlNodeType.Text:
                case XmlNodeType.CDATA:
                    if (insideTargetElement)
                    {
                        currentElement.Append(reader.Value);
                    }
                    break;

                case XmlNodeType.EndElement:
                    if (insideTargetElement && currentTargetPath == string.Join("/", currentPath))
                    {
                        yield return new XmlElementInfo
                        {
                            Path = currentTargetPath,
                            Content = currentElement.ToString().Trim(),
                            Depth = currentPath.Count
                        };
                        insideTargetElement = false;
                        currentTargetPath = null;
                    }

                    elementStack.Pop();
                    currentPath.RemoveAt(currentPath.Count - 1);
                    break;
            }
        }
    }
}

#region 数据模型

/// <summary>
/// XML 元素信息
/// </summary>
internal class XmlElementInfo
{
    public string Path { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Depth { get; set; }
    public Dictionary<string, string>? Attributes { get; set; }
}

/// <summary>
/// 表格行
/// </summary>
internal class TableRow
{
    public string[] Cells { get; set; } = Array.Empty<string>();
}

/// <summary>
/// XML 文档信息
/// </summary>
internal class XmlDocumentInfo
{
    public Dictionary<string, int> ElementCounts { get; set; } = new();
    public int MaxDepth { get; set; }
    public bool HasComplexStructure { get; set; }
    public int TotalElements { get; set; }

    /// <summary>
    /// 判断是否适合使用 DOM 加载
    /// </summary>
    public bool ShouldUseDomLoading()
    {
        // 如果元素数量少且结构不复杂，可以使用 DOM
        return TotalElements < 1000 && MaxDepth < 10;
    }
}

#endregion
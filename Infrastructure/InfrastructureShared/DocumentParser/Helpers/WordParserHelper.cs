using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Models.DocumentParsing;
using Application.Models.DocumentParsing.Node;
using DocumentFormat.OpenXml.Packaging;

namespace Infrastructure.InfrastructureShared.DocumentParser.Helpers;

/// <summary>
/// Word 文档解析辅助类
/// </summary>
internal static class WordParserHelper
{
    /// <summary>
    /// 解析 Word 文档为块
    /// </summary>
    public static async Task<IReadOnlyList<ParsedBlock>> ParseToBlocksAsync(
        Stream stream,
        string fileName,
        DocumentParseOptions? options,
        CancellationToken cancellationToken)
    {
        // 分析文档以确定最佳策略
        var docInfo = await OpenXmlParserHelper.AnalyzeDocumentAsync(
            stream, OpenXmlDocumentType.Word, cancellationToken).ConfigureAwait(false);

        var useStreaming = DocumentLoadStrategy.ShouldUseStreaming(stream) || docInfo.ShouldUseStreaming;
        var blocks = new List<ParsedBlock>();

        // 重置流位置
        if (stream.CanSeek && stream.Position != 0)
            stream.Position = 0;

        try
        {
            await foreach (var paragraph in OpenXmlParserHelper.ReadWordParagraphsAsync(stream, useStreaming, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!string.IsNullOrWhiteSpace(paragraph.Text))
                {
                    var blockType = paragraph.IsHeading ? BlockType.Title : BlockType.Paragraph;
                    blocks.Add(ParsedBlock.CreateTextBlock(paragraph.Text, blockType));
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException($"解析 Word 文件 '{fileName}' 失败: {ex.Message}", ex);
        }

        return blocks;
    }

    /// <summary>
    /// 解析 Word 文档为结构化文档
    /// </summary>
    public static async Task<StructuredDocument> ParseToStructuredAsync(
        Stream stream,
        string fileName,
        DocumentParseOptions? options,
        CancellationToken cancellationToken)
    {
        // 分析文档以确定最佳策略
        var docInfo = await OpenXmlParserHelper.AnalyzeDocumentAsync(
            stream, OpenXmlDocumentType.Word, cancellationToken).ConfigureAwait(false);

        var useStreaming = DocumentLoadStrategy.ShouldUseStreaming(stream) || docInfo.ShouldUseStreaming;
        var structuredDoc = new StructuredDocument
        {
            Metadata = new Dictionary<string, object>
            {
                ["FileName"] = fileName,
                ["ParagraphCount"] = docInfo.ParagraphCount,
                ["HasComplexFormatting"] = docInfo.HasComplexFormatting,
                ["LoadStrategy"] = useStreaming ? "Streaming" : "DOM"
            },
            Nodes = new List<DocumentNode>()
        };

        // 重置流位置
        if (stream.CanSeek && stream.Position != 0)
            stream.Position = 0;

        try
        {
            // 使用状态机构建文档结构
            var documentBuilder = new WordDocumentStructureBuilder();

            await foreach (var paragraph in OpenXmlParserHelper.ReadWordParagraphsAsync(stream, useStreaming, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!string.IsNullOrWhiteSpace(paragraph.Text))
                {
                    documentBuilder.AddParagraph(paragraph.Text, paragraph.IsHeading, paragraph.Style);
                }
            }

            structuredDoc.Nodes = documentBuilder.Build();
            return structuredDoc;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException($"结构化解析 Word 文件 '{fileName}' 失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 快速提取 Word 文档元数据
    /// </summary>
    public static async Task<Dictionary<string, object>> ExtractMetadataAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        var metadata = new Dictionary<string, object>();

        try
        {
            // 重置流位置
            if (stream.CanSeek && stream.Position != 0)
                stream.Position = 0;

            using var wordDoc = WordprocessingDocument.Open(stream, false);
            var mainPart = wordDoc.MainDocumentPart;
            if (mainPart?.Document?.Body == null)
                return metadata;

            // 统计段落数量
            var paragraphCount = mainPart.Document.Body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>().Count();
            metadata["ParagraphCount"] = paragraphCount;

            // 检查是否有表格
            var tableCount = mainPart.Document.Body.Elements<DocumentFormat.OpenXml.Wordprocessing.Table>().Count();
            metadata["TableCount"] = tableCount;

            // 检查是否有图片
            var imageCount = mainPart.ImageParts?.Count() ?? 0;
            metadata["ImageCount"] = imageCount;

            // 获取文档属性
            var coreProperties = wordDoc.PackageProperties;
            if (coreProperties != null)
            {
                metadata["Title"] = coreProperties.Title ?? string.Empty;
                metadata["Author"] = coreProperties.Creator ?? string.Empty;
                metadata["Subject"] = coreProperties.Subject ?? string.Empty;
                metadata["Keywords"] = coreProperties.Keywords ?? string.Empty;
                metadata["Description"] = coreProperties.Description ?? string.Empty;
                metadata["LastModifiedBy"] = coreProperties.LastModifiedBy ?? string.Empty;
                metadata["Revision"] = coreProperties.Revision ?? string.Empty;
                metadata["Created"] = coreProperties.Created?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty;
                metadata["Modified"] = coreProperties.Modified?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty;
            }

            return metadata;
        }
        catch
        {
            // 如果提取元数据失败，返回空字典
            return metadata;
        }
    }

    /// <summary>
    /// Word 文档结构构建器（状态机模式）
    /// </summary>
    private class WordDocumentStructureBuilder
    {
        private readonly List<DocumentNode> _rootNodes = new();
        private readonly Stack<HeadingNode> _headingStack = new();
        private ParagraphNode? _currentParagraph = null;

        public void AddParagraph(string text, bool isHeading, string? style)
        {
            if (isHeading)
            {
                // 完成当前段落
                CompleteCurrentParagraph();

                // 确定标题级别
                var level = DetermineHeadingLevel(style);

                // 创建标题节点
                var headingNode = new HeadingNode
                {
                    Level = level,
                    Text = text,
                    Children = new List<DocumentNode>()
                };

                // 处理标题层级
                while (_headingStack.Count > 0 && _headingStack.Peek().Level >= level)
                {
                    _headingStack.Pop();
                }

                if (_headingStack.Count == 0)
                {
                    _rootNodes.Add(headingNode);
                }
                else
                {
                    _headingStack.Peek().Children.Add(headingNode);
                }

                _headingStack.Push(headingNode);
            }
            else
            {
                // 普通段落
                if (_currentParagraph == null)
                {
                    _currentParagraph = new ParagraphNode
                    {
                        Text = text
                    };
                }
                else
                {
                    // 合并到当前段落（简单实现）
                    _currentParagraph.Text += " " + text;
                }
            }
        }

        public List<DocumentNode> Build()
        {
            // 完成最后一个段落
            CompleteCurrentParagraph();

            // 清理标题栈
            while (_headingStack.Count > 0)
            {
                _headingStack.Pop();
            }

            return _rootNodes;
        }

        private void CompleteCurrentParagraph()
        {
            if (_currentParagraph != null && !string.IsNullOrWhiteSpace(_currentParagraph.Text))
            {
                if (_headingStack.Count > 0)
                {
                    _headingStack.Peek().Children.Add(_currentParagraph);
                }
                else
                {
                    _rootNodes.Add(_currentParagraph);
                }
                _currentParagraph = null;
            }
        }

        private static int DetermineHeadingLevel(string? style)
        {
            if (string.IsNullOrEmpty(style))
                return 1;

            // 根据样式名称判断标题级别
            if (style.Contains("Heading1", StringComparison.OrdinalIgnoreCase) ||
                style.Contains("标题1", StringComparison.OrdinalIgnoreCase))
                return 1;
            if (style.Contains("Heading2", StringComparison.OrdinalIgnoreCase) ||
                style.Contains("标题2", StringComparison.OrdinalIgnoreCase))
                return 2;
            if (style.Contains("Heading3", StringComparison.OrdinalIgnoreCase) ||
                style.Contains("标题3", StringComparison.OrdinalIgnoreCase))
                return 3;
            if (style.Contains("Heading4", StringComparison.OrdinalIgnoreCase) ||
                style.Contains("标题4", StringComparison.OrdinalIgnoreCase))
                return 4;
            if (style.Contains("Heading5", StringComparison.OrdinalIgnoreCase) ||
                style.Contains("标题5", StringComparison.OrdinalIgnoreCase))
                return 5;
            if (style.Contains("Heading6", StringComparison.OrdinalIgnoreCase) ||
                style.Contains("标题6", StringComparison.OrdinalIgnoreCase))
                return 6;

            // 默认标题级别
            return 1;
        }
    }
}
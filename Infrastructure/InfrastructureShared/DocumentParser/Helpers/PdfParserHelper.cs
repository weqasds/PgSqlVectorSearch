using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Application.Models.DocumentParsing;
using Application.Models.DocumentParsing.Node;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Infrastructure.InfrastructureShared.DocumentParser.Helpers;

/// <summary>
/// PDF 解析辅助类，实现流式、高性能解析
/// </summary>
internal static class PdfParserHelper
{
    /// <summary>
    /// 解析 PDF 文档为块
    /// </summary>
    public static async Task<IReadOnlyList<ParsedBlock>> ParseToBlocksAsync(
        Stream stream,
        string fileName,
        DocumentParseOptions? options,
        CancellationToken cancellationToken)
    {
        // 使用 Task.Run 将 CPU 密集型操作卸载到线程池
        return await Task.Run(() =>
        {
            var blocks = new List<ParsedBlock>();

            // 重置流位置（如果需要）
            if (stream.CanSeek && stream.Position != 0)
                stream.Position = 0;

            using var document = PdfDocument.Open(stream);

            // 确定页码范围
            var (startPage, endPage) = GetPageRange(document, options);

            for (int pageNumber = startPage; pageNumber <= endPage; pageNumber++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var page = document.GetPage(pageNumber);
                var pageBlocks = ProcessPage(page, pageNumber, options);
                blocks.AddRange(pageBlocks);
            }

            return (IReadOnlyList<ParsedBlock>)blocks;
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 解析 PDF 文档为结构化文档
    /// </summary>
    public static async Task<StructuredDocument> ParseToStructuredAsync(
        Stream stream,
        string fileName,
        DocumentParseOptions? options,
        CancellationToken cancellationToken)
    {
        // 使用 Task.Run 将 CPU 密集型操作卸载到线程池
        return await Task.Run(() =>
        {
            var structuredDoc = new StructuredDocument
            {
                Metadata = new Dictionary<string, object>()
            };

            // 重置流位置
            if (stream.CanSeek && stream.Position != 0)
                stream.Position = 0;

            using var document = PdfDocument.Open(stream);

            // 获取文档信息
            structuredDoc.Metadata["Author"] = document.Information.Author ?? string.Empty;
            structuredDoc.Metadata["Title"] = document.Information.Title ?? string.Empty;
            structuredDoc.Metadata["Subject"] = document.Information.Subject ?? string.Empty;
            structuredDoc.Metadata["Keywords"] = document.Information.Keywords ?? string.Empty;
            structuredDoc.Metadata["Creator"] = document.Information.Creator ?? string.Empty;
            structuredDoc.Metadata["Producer"] = document.Information.Producer ?? string.Empty;
            structuredDoc.Metadata["CreationDate"] = document.Information.CreationDate?.ToString() ?? string.Empty;
            structuredDoc.Metadata["ModifiedDate"] = document.Information.ModifiedDate?.ToString() ?? string.Empty;

            var (startPage, endPage) = GetPageRange(document, options);

            // 使用状态机构建文档结构
            var documentBuilder = new PdfDocumentStructureBuilder();

            for (int pageNumber = startPage; pageNumber <= endPage; pageNumber++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var page = document.GetPage(pageNumber);
                ProcessPageForStructure(page, pageNumber, documentBuilder, options);
            }

            structuredDoc.Nodes = documentBuilder.Build();
            return structuredDoc;
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 处理单个页面，生成块
    /// </summary>
    private static List<ParsedBlock> ProcessPage(
        Page page,
        int pageNumber,
        DocumentParseOptions? options)
    {
        var blocks = new List<ParsedBlock>();

        // 字符级缓存：一次性获取所有字符
        var letters = page.Letters.ToList();

        // 如果没有字符，返回空列表
        if (letters.Count == 0)
            return blocks;

        // 按 Y 坐标分组构建行（保留布局）
        var lines = BuildLinesFromLetters(letters, options?.PreserveLayout ?? true);

        // 将行合并为段落
        var paragraphs = MergeLinesIntoParagraphs(lines);

        // 创建块
        foreach (var paragraph in paragraphs)
        {
            if (string.IsNullOrWhiteSpace(paragraph.Text))
                continue;

            blocks.Add(ParsedBlock.CreateTextBlock(
                paragraph.Text,
                paragraph.IsTitle ? BlockType.Title : BlockType.Paragraph,
                pageNumber
            ));
        }

        // 提取表格（如果启用）
        if (options?.ExtractTables ?? true)
        {
            var tableBlocks = ExtractTablesFromPage(page, pageNumber, options);
            blocks.AddRange(tableBlocks);
        }

        return blocks;
    }

    /// <summary>
    /// 处理单个页面，用于结构化文档构建
    /// </summary>
    private static void ProcessPageForStructure(
        Page page,
        int pageNumber,
        PdfDocumentStructureBuilder builder,
        DocumentParseOptions? options)
    {
        var letters = page.Letters.ToList();
        if (letters.Count == 0)
            return;

        var lines = BuildLinesFromLetters(letters, options?.PreserveLayout ?? true);
        builder.AddPage(lines, pageNumber);
    }

    /// <summary>
    /// 从字符构建行
    /// </summary>
    private static List<TextLine> BuildLinesFromLetters(
        List<Letter> letters,
        bool preserveLayout)
    {
        if (letters.Count == 0)
            return new List<TextLine>();

        // 按 Y 坐标分组（考虑行高容差）
        var tolerance = preserveLayout ? 0.5 : 2.0; // 布局保留时使用更小的容差
        var lineGroups = letters
            .GroupBy(l => Math.Round(l.GlyphRectangle.Bottom / tolerance) * tolerance)
            .OrderByDescending(g => g.Key) // PDF 坐标：从上到下
            .ToList();

        var lines = new List<TextLine>();

        foreach (var group in lineGroups)
        {
            // 按 X 坐标排序字符
            var sortedLetters = group.OrderBy(l => l.GlyphRectangle.Left).ToList();

            // 构建行文本
            var lineText = string.Concat(sortedLetters.Select(l => l.Value));

            // 计算平均字体大小（用于标题检测）
            var avgFontSize = sortedLetters.Average(l => l.FontSize);
            var maxFontSize = sortedLetters.Max(l => l.FontSize);

            lines.Add(new TextLine
            {
                Text = lineText,
                AverageFontSize = avgFontSize,
                MaxFontSize = maxFontSize,
                IsBold = sortedLetters.Any(l => l.FontName?.Contains("Bold", StringComparison.OrdinalIgnoreCase) == true),
                IsItalic = sortedLetters.Any(l => l.FontName?.Contains("Italic", StringComparison.OrdinalIgnoreCase) == true)
            });
        }

        return lines;
    }

    /// <summary>
    /// 将行合并为段落
    /// </summary>
    private static List<ParagraphInfo> MergeLinesIntoParagraphs(List<TextLine> lines)
    {
        if (lines.Count == 0)
            return new List<ParagraphInfo>();

        var paragraphs = new List<ParagraphInfo>();
        var currentParagraph = new StringBuilder();
        var currentIsTitle = false;
        var titleFontSizeThreshold = lines.Average(l => l.MaxFontSize) * 1.2; // 标题字体大小阈值

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var isTitle = line.MaxFontSize > titleFontSizeThreshold || line.IsBold;

            // 判断是否开始新段落：空行或样式变化
            bool startNewParagraph = false;
            if (i == 0)
            {
                startNewParagraph = true;
            }
            else
            {
                var prevLine = lines[i - 1];
                var isPrevTitle = prevLine.MaxFontSize > titleFontSizeThreshold || prevLine.IsBold;

                // 标题与正文切换，或空行分隔
                if (isTitle != isPrevTitle || string.IsNullOrWhiteSpace(prevLine.Text))
                {
                    startNewParagraph = true;
                }
            }

            if (startNewParagraph && currentParagraph.Length > 0)
            {
                paragraphs.Add(new ParagraphInfo
                {
                    Text = currentParagraph.ToString().Trim(),
                    IsTitle = currentIsTitle
                });
                currentParagraph.Clear();
            }

            if (startNewParagraph)
            {
                currentIsTitle = isTitle;
            }

            if (!string.IsNullOrWhiteSpace(line.Text))
            {
                if (currentParagraph.Length > 0)
                    currentParagraph.Append(' ');
                currentParagraph.Append(line.Text);
            }
        }

        // 添加最后一个段落
        if (currentParagraph.Length > 0)
        {
            paragraphs.Add(new ParagraphInfo
            {
                Text = currentParagraph.ToString().Trim(),
                IsTitle = currentIsTitle
            });
        }

        return paragraphs;
    }

    /// <summary>
    /// 从页面提取表格（简化实现）
    /// </summary>
    private static List<ParsedBlock> ExtractTablesFromPage(
        Page page,
        int pageNumber,
        DocumentParseOptions? options)
    {
        // 这里可以实现更复杂的表格检测逻辑
        // 目前返回空列表，后续可以扩展
        return new List<ParsedBlock>();
    }

    /// <summary>
    /// 获取页码范围
    /// </summary>
    private static (int startPage, int endPage) GetPageRange(
        PdfDocument document,
        DocumentParseOptions? options)
    {
        var totalPages = document.NumberOfPages;
        var startPage = options?.StartPage ?? 1;
        var endPage = options?.EndPage ?? totalPages;

        // 边界检查
        startPage = Math.Max(1, startPage);
        endPage = Math.Min(totalPages, endPage);

        if (startPage > endPage)
            startPage = endPage;

        return (startPage, endPage);
    }

    #region 内部辅助类

    private class TextLine
    {
        public string Text { get; set; } = string.Empty;
        public double AverageFontSize { get; set; }
        public double MaxFontSize { get; set; }
        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }
    }

    private class ParagraphInfo
    {
        public string Text { get; set; } = string.Empty;
        public bool IsTitle { get; set; }
    }

    /// <summary>
    /// PDF 文档结构构建器（状态机模式）
    /// </summary>
    private class PdfDocumentStructureBuilder
    {
        private readonly List<DocumentNode> _rootNodes = new();
        private HeadingNode? _currentHeading = null;
        private readonly Stack<HeadingNode> _headingStack = new();

        public void AddPage(List<TextLine> lines, int pageNumber)
        {
            // 实现文档结构构建逻辑
            // 根据字体大小、样式等识别标题层级
            // 将段落添加到相应的标题节点下
            // 这里需要更复杂的实现，暂时简化
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line.Text))
                {
                    var paragraphNode = new ParagraphNode
                    {
                        Text = line.Text,
                        PageNumber = pageNumber
                    };

                    if (_currentHeading != null)
                    {
                        _currentHeading.Children.Add(paragraphNode);
                    }
                    else
                    {
                        _rootNodes.Add(paragraphNode);
                    }
                }
            }
        }

        public List<DocumentNode> Build()
        {
            return _rootNodes;
        }
    }

    #endregion
}
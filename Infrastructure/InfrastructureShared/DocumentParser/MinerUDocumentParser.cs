using System.Collections.Generic;
using System.IO;
using System.Linq;
using Application.ApplicationServiceContract.DocumentParsing;
using Application.Models.DocumentParsing;
using Application.Models.DocumentParsing.Node;
using DomainService.Exceptions;
using Microsoft.Extensions.Options;

namespace Infrastructure.InfrastructureShared.DocumentParser;

/// <summary>
/// 基于 MinerU 精准解析 API 的文档解析器。
/// </summary>
public class MinerUDocumentParser : DocumentParserBase, IChunkableDocumentParser, IStructuredDocumentParser
{
    private readonly MinerUDocumentParserClient _client;
    private readonly MinerUDocumentParserOptions _options;

    public MinerUDocumentParser(
        MinerUDocumentParserClient client,
        IOptions<MinerUDocumentParserOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    protected internal override IReadOnlySet<string> SupportedExtensions { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf",
            ".png", ".jpg", ".jpeg", ".jp2", ".webp", ".gif", ".bmp",
            ".doc", ".docx",
            ".ppt", ".pptx",
            ".xls", ".xlsx",
            ".html", ".htm"
        };

    public override bool CanHandle(string fileName)
    {
        return _options.Enabled &&
               !string.IsNullOrWhiteSpace(_options.BaseUrl) &&
               !string.IsNullOrWhiteSpace(_options.BearerToken) &&
               base.CanHandle(fileName);
    }

    protected override async Task<ParsedDocument> ParseCoreAsync(
        Stream stream,
        string fileName,
        DocumentParseOptions? options,
        CancellationToken cancellationToken)
    {
        var contentList = await _client.ParseAsync(stream, fileName, options, cancellationToken)
            .ConfigureAwait(false);

        return MapToParsedDocument(contentList, options);
    }

    public async Task<IReadOnlyList<ParsedBlock>> ParseBlocksAsync(
        Stream stream,
        string fileName,
        DocumentParseOptions? documentParseOptions = null,
        CancellationToken cancellationToken = default)
    {
        var parsedDocument = await ParseAsync(stream, fileName, documentParseOptions, cancellationToken)
            .ConfigureAwait(false);
        return parsedDocument.Blocks;
    }

    public async Task<StructuredDocument> ParseStructuredAsync(
        Stream stream,
        string fileName,
        DocumentParseOptions? documentParseOptions = null,
        CancellationToken cancellationToken = default)
    {
        ValidateArguments(stream, fileName);

        if (!CanHandle(fileName))
        {
            throw new DocumentFormatNotSupportedException(
                $"文件 '{fileName}' 的格式不被支持。本解析器支持: {string.Join(", ", SupportedExtensions)}")
            {
                FileName = fileName,
                SupportedFormats = string.Join(", ", SupportedExtensions)
            };
        }

        try
        {
            var contentList = await _client.ParseAsync(stream, fileName, documentParseOptions, cancellationToken)
                .ConfigureAwait(false);
            return MapToStructuredDocument(contentList, documentParseOptions);
        }
        catch (DocumentFormatNotSupportedException)
        {
            throw;
        }
        catch (DocumentParsingFailedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new DocumentParsingFailedException(
                $"结构化解析文件 '{fileName}' 时发生错误。", ex)
            {
                FileName = fileName,
                ParserType = GetType().Name
            };
        }
    }

    private static ParsedDocument MapToParsedDocument(
        List<MinerUContentBlock> contentList,
        DocumentParseOptions? options)
    {
        var blocks = BuildBlocks(contentList, options);
        var fullText = string.Join(
            "\n\n",
            blocks.Select(block => block.Text).Where(text => !string.IsNullOrWhiteSpace(text)));

        return new ParsedDocument
        {
            FullText = fullText,
            Blocks = blocks,
            Metadata = BuildMetadata(contentList)
        };
    }

    private static StructuredDocument MapToStructuredDocument(
        List<MinerUContentBlock> contentList,
        DocumentParseOptions? options)
    {
        var nodes = BuildStructuredNodes(contentList, options);

        if (nodes.Count == 0)
        {
            // 回退：将所有文本块作为段落节点
            nodes = BuildBlocks(contentList, options)
                .Where(block => !string.IsNullOrWhiteSpace(block.Text))
                .Select(block => (DocumentNode)new ParagraphNode
                {
                    Text = block.Text,
                    PageNumber = block.PageNumber
                })
                .ToList();
        }

        return new StructuredDocument
        {
            Title = null, // TODO: 从元数据中提取标题
            Nodes = nodes,
            Metadata = BuildMetadata(contentList)
        };
    }

    private static List<ParsedBlock> BuildBlocks(
        List<MinerUContentBlock> contentList,
        DocumentParseOptions? options)
    {
        var selectedPages = BuildSelectedPageSet(contentList, options);
        var blocks = new List<ParsedBlock>();

        foreach (var block in contentList)
        {
            if (!selectedPages.Contains(block.PageIdx))
            {
                continue;
            }

            switch (block)
            {
                case MinerUTextBlock textBlock:
                    var blockType = textBlock.TextLevel == 0
                        ? BlockType.Paragraph
                        : BlockType.Title;
                    blocks.Add(new ParsedBlock
                    {
                        Text = textBlock.Text.Trim(),
                        Type = blockType,
                        PageNumber = textBlock.PageIdx + 1, // 转换为从1开始
                        Metadata = CreateMetadataDictionary(("textLevel", textBlock.TextLevel))
                    });
                    break;

                case MinerUTableBlock tableBlock:
                    var tableText = ConvertTableToText(tableBlock);
                    if (!string.IsNullOrWhiteSpace(tableText))
                    {
                        blocks.Add(new ParsedBlock
                        {
                            Text = tableText,
                            Type = BlockType.Table,
                            PageNumber = tableBlock.PageIdx + 1,
                            Metadata = CreateMetadataDictionary(
                                ("imgPath", tableBlock.ImgPath),
                                ("caption", string.Join("; ", tableBlock.TableCaption)))
                        });
                    }
                    break;

                case MinerUEquationBlock equationBlock:
                    // 公式作为特殊段落处理
                    blocks.Add(new ParsedBlock
                    {
                        Text = $"公式: {equationBlock.Text}",
                        Type = BlockType.Paragraph,
                        PageNumber = equationBlock.PageIdx + 1,
                        Metadata = CreateMetadataDictionary(("type", "equation"))
                    });
                    break;

                case MinerUCodeBlock codeBlock:
                    blocks.Add(new ParsedBlock
                    {
                        Text = $"代码: {codeBlock.CodeBody}",
                        Type = BlockType.Paragraph,
                        PageNumber = codeBlock.PageIdx + 1,
                        Metadata = CreateMetadataDictionary(("type", "code"), ("subType", codeBlock.SubType))
                    });
                    break;

                case MinerUListBlock listBlock:
                    var listText = string.Join("\n", listBlock.ListItems.Select((item, idx) => $"{idx + 1}. {item}"));
                    blocks.Add(new ParsedBlock
                    {
                        Text = listText,
                        Type = BlockType.Paragraph,
                        PageNumber = listBlock.PageIdx + 1,
                        Metadata = CreateMetadataDictionary(("type", "list"), ("subType", listBlock.SubType))
                    });
                    break;

                case MinerUImageBlock imageBlock:
                    // 图片作为特殊段落处理
                    blocks.Add(new ParsedBlock
                    {
                        Text = $"图片: {imageBlock.ImgPath}",
                        Type = BlockType.Paragraph,
                        PageNumber = imageBlock.PageIdx + 1,
                        Metadata = CreateMetadataDictionary(("type", "image"))
                    });
                    break;

                // 忽略废弃块
                case MinerUDiscardedBlock discardedBlock:
                    // 跳过
                    break;
            }
        }

        if (blocks.Count > 0)
        {
            return blocks
                .OrderBy(block => block.PageNumber ?? int.MaxValue)
                .ThenBy(block => block.Type == BlockType.Title ? 0 : block.Type == BlockType.Paragraph ? 1 : 2)
                .ToList();
        }

        // 回退：将所有文本内容作为段落块
        return contentList
            .OfType<MinerUTextBlock>()
            .Where(textBlock => selectedPages.Contains(textBlock.PageIdx) && !string.IsNullOrWhiteSpace(textBlock.Text))
            .Select(textBlock => new ParsedBlock
            {
                Text = textBlock.Text.Trim(),
                Type = BlockType.Paragraph,
                PageNumber = textBlock.PageIdx + 1
            })
            .ToList();
    }

    private static List<DocumentNode> BuildStructuredNodes(
        List<MinerUContentBlock> contentList,
        DocumentParseOptions? options)
    {
        var selectedPages = BuildSelectedPageSet(contentList, options);
        var items = new List<StructuredSequenceItem>();

        foreach (var block in contentList)
        {
            if (!selectedPages.Contains(block.PageIdx))
            {
                continue;
            }

            switch (block)
            {
                case MinerUTextBlock textBlock:
                    items.Add(new StructuredSequenceItem
                    {
                        Kind = textBlock.TextLevel == 0 ? StructuredItemKind.Paragraph : StructuredItemKind.Heading,
                        Page = textBlock.PageIdx,
                        SortY = textBlock.Bbox?.Y0,
                        Text = textBlock.Text,
                        TextLevel = textBlock.TextLevel
                    });
                    break;

                case MinerUTableBlock tableBlock:
                    items.Add(new StructuredSequenceItem
                    {
                        Kind = StructuredItemKind.Table,
                        Page = tableBlock.PageIdx,
                        SortY = tableBlock.Bbox?.Y0,
                        TableBody = tableBlock.TableBody
                    });
                    break;
            }
        }

        var orderedItems = items
            .OrderBy(item => item.Page)
            .ThenBy(item => item.SortY ?? double.MaxValue)
            .ToList();

        var roots = new List<DocumentNode>();
        var headingStack = new Stack<HeadingNode>();

        foreach (var item in orderedItems)
        {
            switch (item.Kind)
            {
                case StructuredItemKind.Heading:
                    var heading = new HeadingNode
                    {
                        Text = item.Text!.Trim(),
                        Level = Math.Clamp(item.TextLevel, 1, 6),
                        PageNumber = item.Page + 1
                    };

                    while (headingStack.Count > 0 && headingStack.Peek().Level >= heading.Level)
                    {
                        headingStack.Pop();
                    }

                    if (headingStack.Count > 0)
                    {
                        headingStack.Peek().Children.Add(heading);
                    }
                    else
                    {
                        roots.Add(heading);
                    }

                    headingStack.Push(heading);
                    break;

                case StructuredItemKind.Paragraph:
                    if (string.IsNullOrWhiteSpace(item.Text))
                    {
                        break;
                    }

                    AttachNode(
                        new ParagraphNode
                        {
                            Text = item.Text.Trim(),
                            PageNumber = item.Page + 1
                        },
                        roots,
                        headingStack);
                    break;

                case StructuredItemKind.Table:
                    var tableNode = new TableNode
                    {
                        // TODO: 从 HTML 表格中提取表头和行
                        Headers = new List<string>(),
                        Rows = new List<List<string>>(),
                        PageNumber = item.Page + 1,
                        Metadata = CreateMetadataDictionary(("html", item.TableBody))
                    };

                    AttachNode(tableNode, roots, headingStack);
                    break;
            }
        }

        return roots;
    }

    private static void AttachNode(
        DocumentNode node,
        ICollection<DocumentNode> roots,
        Stack<HeadingNode> headingStack)
    {
        if (headingStack.Count > 0)
        {
            headingStack.Peek().Children.Add(node);
        }
        else
        {
            roots.Add(node);
        }
    }

    private static HashSet<int> BuildSelectedPageSet(
        List<MinerUContentBlock> contentList,
        DocumentParseOptions? options)
    {
        var maxPage = contentList.Select(b => b.PageIdx).DefaultIfEmpty(-1).Max();
        if (maxPage < 0)
        {
            return new HashSet<int>();
        }

        var startPage = Math.Max(options?.StartPage ?? 1, 1) - 1; // 转换为从0开始
        var endPage = options?.EndPage.HasValue == true
            ? options.EndPage.Value - 1
            : maxPage;

        return Enumerable.Range(startPage, endPage - startPage + 1)
            .Where(page => page >= 0 && page <= maxPage)
            .ToHashSet();
    }

    private static Dictionary<string, object> BuildMetadata(List<MinerUContentBlock> contentList)
    {
        var metadata = new Dictionary<string, object>();

        var textBlocks = contentList.OfType<MinerUTextBlock>().ToList();
        var tableBlocks = contentList.OfType<MinerUTableBlock>().ToList();
        var imageBlocks = contentList.OfType<MinerUImageBlock>().ToList();
        var equationBlocks = contentList.OfType<MinerUEquationBlock>().ToList();
        var codeBlocks = contentList.OfType<MinerUCodeBlock>().ToList();
        var listBlocks = contentList.OfType<MinerUListBlock>().ToList();

        metadata["TotalBlocks"] = contentList.Count;
        metadata["TextBlocks"] = textBlocks.Count;
        metadata["TableBlocks"] = tableBlocks.Count;
        metadata["ImageBlocks"] = imageBlocks.Count;
        metadata["EquationBlocks"] = equationBlocks.Count;
        metadata["CodeBlocks"] = codeBlocks.Count;
        metadata["ListBlocks"] = listBlocks.Count;

        var pageCount = contentList.Select(b => b.PageIdx).DefaultIfEmpty(-1).Max() + 1;
        if (pageCount > 0)
        {
            metadata["PageCount"] = pageCount;
        }

        return metadata;
    }

    private static Dictionary<string, object>? CreateMetadataDictionary(params (string key, object? value)[] items)
    {
        var metadata = new Dictionary<string, object>();
        foreach (var (key, value) in items)
        {
            if (value is not null)
            {
                metadata[key] = value;
            }
        }

        return metadata.Count == 0 ? null : metadata;
    }

    private static string ConvertTableToText(MinerUTableBlock tableBlock)
    {
        // 如果 TableBody 是 HTML，可以尝试提取文本
        // 简化版：返回占位符
        return $"表格（{tableBlock.TableCaption.FirstOrDefault() ?? "无标题"}）";
    }

    private enum StructuredItemKind
    {
        Heading,
        Paragraph,
        Table
    }

    private sealed class StructuredSequenceItem
    {
        public StructuredItemKind Kind { get; set; }
        public int Page { get; set; }
        public double? SortY { get; set; }
        public string? Text { get; set; }
        public int TextLevel { get; set; }
        public string? TableBody { get; set; }
    }
}
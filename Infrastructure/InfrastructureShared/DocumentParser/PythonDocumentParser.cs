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
/// 基于 Python 外部服务的文档解析器。
/// 仅接管 PDF 和旧版二进制 Office 文档。
/// </summary>
public class PythonDocumentParser : DocumentParserBase, IChunkableDocumentParser, IStructuredDocumentParser
{
    private readonly PythonDocumentParserClient _client;
    private readonly PythonDocumentParserOptions _options;

    public PythonDocumentParser(
        PythonDocumentParserClient client,
        IOptions<PythonDocumentParserOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    protected internal override IReadOnlySet<string> SupportedExtensions { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf",
            ".doc",
            ".xls",
            ".ppt"
        };

    public override bool CanHandle(string fileName)
    {
        return _options.Enabled &&
               !string.IsNullOrWhiteSpace(_options.BaseUrl) &&
               base.CanHandle(fileName);
    }

    protected override async Task<ParsedDocument> ParseCoreAsync(
        Stream stream,
        string fileName,
        DocumentParseOptions? options,
        CancellationToken cancellationToken)
    {
        var envelope = await _client.ParseAsync(stream, fileName, options, cancellationToken)
            .ConfigureAwait(false);

        return MapToParsedDocument(envelope, options);
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
            var envelope = await _client.ParseAsync(stream, fileName, documentParseOptions, cancellationToken)
                .ConfigureAwait(false);
            return MapToStructuredDocument(envelope, documentParseOptions);
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
        PythonParseEnvelope envelope,
        DocumentParseOptions? options)
    {
        var blocks = BuildBlocks(envelope.Data!, options);
        var selectedPages = GetSelectedPages(envelope.Data!, options).ToList();
        var fullText = string.Join(
            "\n\n",
            selectedPages
                .Select(page => page.Text?.Trim())
                .Where(text => !string.IsNullOrWhiteSpace(text)));

        if (string.IsNullOrWhiteSpace(fullText))
        {
            fullText = string.Join(
                "\n\n",
                blocks.Select(block => block.Text).Where(text => !string.IsNullOrWhiteSpace(text)));
        }

        return new ParsedDocument
        {
            FullText = fullText,
            Blocks = blocks,
            Metadata = BuildMetadata(envelope)
        };
    }

    private static StructuredDocument MapToStructuredDocument(
        PythonParseEnvelope envelope,
        DocumentParseOptions? options)
    {
        var nodes = BuildStructuredNodes(envelope.Data!, options);

        if (nodes.Count == 0)
        {
            nodes = GetSelectedPages(envelope.Data!, options)
                .Where(page => !string.IsNullOrWhiteSpace(page.Text))
                .Select(page => (DocumentNode)new ParagraphNode
                {
                    Text = page.Text.Trim(),
                    PageNumber = page.Page
                })
                .ToList();
        }

        return new StructuredDocument
        {
            Title = envelope.Data?.DocumentMetadata?.Title,
            Nodes = nodes,
            Metadata = BuildMetadata(envelope)
        };
    }

    private static List<ParsedBlock> BuildBlocks(
        PythonParseResult parseResult,
        DocumentParseOptions? options)
    {
        var selectedPages = BuildSelectedPageSet(parseResult, options);
        var blocks = new List<ParsedBlock>();

        foreach (var heading in parseResult.Structure.Headings.Where(h => selectedPages.Contains(h.Page)))
        {
            if (!string.IsNullOrWhiteSpace(heading.Text))
            {
                blocks.Add(new ParsedBlock
                {
                    Text = heading.Text.Trim(),
                    Type = BlockType.Title,
                    PageNumber = heading.Page,
                    Metadata = CreateMetadataDictionary(("level", heading.Level))
                });
            }
        }

        foreach (var paragraph in parseResult.Structure.Paragraphs.Where(p => selectedPages.Contains(p.Page)))
        {
            if (!string.IsNullOrWhiteSpace(paragraph.Text))
            {
                blocks.Add(new ParsedBlock
                {
                    Text = paragraph.Text.Trim(),
                    Type = BlockType.Paragraph,
                    PageNumber = paragraph.Page
                });
            }
        }

        foreach (var page in parseResult.Content.Where(page => selectedPages.Contains(page.Page)))
        {
            foreach (var table in page.Tables)
            {
                var tableText = ConvertTableToText(table);
                if (!string.IsNullOrWhiteSpace(tableText))
                {
                    blocks.Add(new ParsedBlock
                    {
                        Text = tableText,
                        Type = BlockType.Table,
                        PageNumber = page.Page,
                        Metadata = CreateMetadataDictionary(("tableIndex", table.TableIndex))
                    });
                }
            }
        }

        if (blocks.Count > 0)
        {
            return blocks
                .OrderBy(block => block.PageNumber ?? int.MaxValue)
                .ThenBy(block => block.Type == BlockType.Title ? 0 : block.Type == BlockType.Paragraph ? 1 : 2)
                .ToList();
        }

        return GetSelectedPages(parseResult, options)
            .Where(page => !string.IsNullOrWhiteSpace(page.Text))
            .Select(page => new ParsedBlock
            {
                Text = page.Text.Trim(),
                Type = BlockType.Paragraph,
                PageNumber = page.Page
            })
            .ToList();
    }

    private static List<DocumentNode> BuildStructuredNodes(
        PythonParseResult parseResult,
        DocumentParseOptions? options)
    {
        var selectedPages = BuildSelectedPageSet(parseResult, options);
        var items = new List<StructuredSequenceItem>();
        var order = 0;

        foreach (var heading in parseResult.Structure.Headings.Where(h => selectedPages.Contains(h.Page)))
        {
            items.Add(new StructuredSequenceItem
            {
                Kind = StructuredItemKind.Heading,
                Page = heading.Page,
                SortY = heading.Bbox?.Y1,
                Order = order++,
                Heading = heading
            });
        }

        foreach (var paragraph in parseResult.Structure.Paragraphs.Where(p => selectedPages.Contains(p.Page)))
        {
            items.Add(new StructuredSequenceItem
            {
                Kind = StructuredItemKind.Paragraph,
                Page = paragraph.Page,
                SortY = paragraph.Bbox?.Y1,
                Order = order++,
                Paragraph = paragraph
            });
        }

        foreach (var page in parseResult.Content.Where(page => selectedPages.Contains(page.Page)))
        {
            foreach (var table in page.Tables)
            {
                items.Add(new StructuredSequenceItem
                {
                    Kind = StructuredItemKind.Table,
                    Page = page.Page,
                    SortY = table.Bbox?.Y1,
                    Order = order++,
                    Table = table
                });
            }
        }

        var orderedItems = items
            .OrderBy(item => item.Page)
            .ThenBy(item => item.SortY ?? double.MaxValue)
            .ThenBy(item => item.Order)
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
                        Text = item.Heading!.Text.Trim(),
                        Level = Math.Clamp(item.Heading.Level, 1, 6),
                        PageNumber = item.Page
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
                    if (string.IsNullOrWhiteSpace(item.Paragraph!.Text))
                    {
                        break;
                    }

                    AttachNode(
                        new ParagraphNode
                        {
                            Text = item.Paragraph.Text.Trim(),
                            PageNumber = item.Page
                        },
                        roots,
                        headingStack);
                    break;

                case StructuredItemKind.Table:
                    var rows = item.Table!.Rows ?? new List<List<string>>();
                    if (rows.Count == 0)
                    {
                        break;
                    }

                    var tableNode = new TableNode
                    {
                        Headers = rows.FirstOrDefault()?.ToList(),
                        Rows = rows.Skip(1).Select(row => row.ToList()).ToList(),
                        PageNumber = item.Page,
                        Metadata = CreateMetadataDictionary(("tableIndex", item.Table.TableIndex))
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

    private static IEnumerable<PythonPageContent> GetSelectedPages(
        PythonParseResult parseResult,
        DocumentParseOptions? options)
    {
        var selectedPages = BuildSelectedPageSet(parseResult, options);
        return parseResult.Content.Where(page => selectedPages.Contains(page.Page));
    }

    private static HashSet<int> BuildSelectedPageSet(
        PythonParseResult parseResult,
        DocumentParseOptions? options)
    {
        var startPage = Math.Max(options?.StartPage ?? 1, 1);
        var endPage = options?.EndPage ?? parseResult.DocumentMetadata.PageCount;

        return parseResult.Content
            .Select(page => page.Page)
            .Where(page => page >= startPage && page <= endPage)
            .ToHashSet();
    }

    private static Dictionary<string, object> BuildMetadata(PythonParseEnvelope envelope)
    {
        var metadata = new Dictionary<string, object>();
        var documentMetadata = envelope.Data!.DocumentMetadata;

        AddIfNotEmpty(metadata, "Title", documentMetadata.Title);
        AddIfNotEmpty(metadata, "Author", documentMetadata.Author);
        AddIfNotEmpty(metadata, "Subject", documentMetadata.Subject);
        if (documentMetadata.Keywords.Count > 0)
        {
            metadata["Keywords"] = documentMetadata.Keywords;
        }
        if (documentMetadata.CreatedDate.HasValue)
        {
            metadata["CreatedDate"] = documentMetadata.CreatedDate.Value;
        }
        if (documentMetadata.ModifiedDate.HasValue)
        {
            metadata["ModifiedDate"] = documentMetadata.ModifiedDate.Value;
        }

        metadata["PageCount"] = documentMetadata.PageCount;
        metadata["FileType"] = documentMetadata.FileType;
        metadata["OcrTriggered"] = envelope.Data.OcrTriggered;

        if (documentMetadata.FileSize.HasValue)
        {
            metadata["FileSize"] = documentMetadata.FileSize.Value;
        }

        if (envelope.Metadata?.ProcessingTime is double processingTime)
        {
            metadata["PythonProcessingTime"] = processingTime;
        }

        AddIfNotEmpty(metadata, "PythonStrategy", envelope.Metadata?.StrategyUsed);
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

    private static string ConvertTableToText(PythonTable table)
    {
        return string.Join(
            "\n",
            table.Rows
                .Where(row => row.Count > 0)
                .Select(row => string.Join(" | ", row)));
    }

    private static void AddIfNotEmpty(
        IDictionary<string, object> metadata,
        string key,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            metadata[key] = value;
        }
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
        public int Order { get; set; }
        public PythonHeading? Heading { get; set; }
        public PythonParagraph? Paragraph { get; set; }
        public PythonTable? Table { get; set; }
    }
}

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Application.ApplicationServiceContract.DocumentParsing;
using Application.Models.DocumentParsing;
using Application.Models.DocumentParsing.Node;
using DomainService.Exceptions;

namespace Infrastructure.InfrastructureShared.DocumentParser;

/// <summary>
/// 支持结构化解析的文档解析器基类。
/// </summary>
public abstract class StructuredDocumentParserBase : DocumentParserBase, IStructuredDocumentParser
{
    /// <summary>
    /// 结构化解析文档的实现。
    /// </summary>
    protected abstract Task<StructuredDocument> ParseStructuredCoreAsync(
        Stream stream,
        string fileName,
        DocumentParseOptions? options,
        CancellationToken cancellationToken);

    /// <inheritdoc />
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
            return await ParseStructuredCoreAsync(stream, fileName, documentParseOptions, cancellationToken)
                .ConfigureAwait(false);
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

    /// <summary>
    /// 默认实现：调用结构化解析方法。
    /// 子类可以重写此方法以提供更高效的完整文档解析。
    /// </summary>
    protected override async Task<ParsedDocument> ParseCoreAsync(
        Stream stream,
        string fileName,
        DocumentParseOptions? options,
        CancellationToken cancellationToken)
    {
        var structured = await ParseStructuredAsync(stream, fileName, options, cancellationToken);
        return ConvertToParsedDocument(structured);
    }

    /// <summary>
    /// 将结构化文档转换为通用 ParsedDocument
    /// </summary>
    protected virtual ParsedDocument ConvertToParsedDocument(StructuredDocument structured)
    {
        var blocks = new List<ParsedBlock>();

        FlattenNodes(structured.Nodes, blocks);

        return new ParsedDocument
        {
            FullText = string.Join("\n\n", blocks.Select(b => b.Text)),
            Blocks = blocks,
            Metadata = structured.Metadata
        };
    }
    private void FlattenNodes(IEnumerable<DocumentNode> nodes, List<ParsedBlock> blocks)
    {
        foreach (var node in nodes)
        {
            switch (node)
            {
                case HeadingNode heading:
                    blocks.Add(new ParsedBlock
                    {
                        Text = heading.Text,
                        Type = BlockType.Title,
                        PageNumber = heading.PageNumber
                    });
                    break;

                case ParagraphNode paragraph:
                    blocks.Add(new ParsedBlock
                    {
                        Text = paragraph.Text,
                        Type = BlockType.Paragraph,
                        PageNumber = paragraph.PageNumber
                    });
                    break;

                case TableNode table:
                    blocks.Add(new ParsedBlock
                    {
                        Text = ConvertTableToText(table),
                        Type = BlockType.Table,
                        PageNumber = table.PageNumber
                    });
                    break;
            }

            if (node.Children.Count > 0)
            {
                FlattenNodes(node.Children, blocks);
            }
        }
    }
    protected virtual string ConvertTableToText(TableNode table)
    {
        var lines = new List<string>();

        if (table.Headers != null)
            lines.Add(string.Join(" | ", table.Headers));

        lines.AddRange(table.Rows.Select(r => string.Join(" | ", r)));

        return string.Join("\n", lines);
    }

}
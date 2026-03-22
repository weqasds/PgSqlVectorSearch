using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Application.ApplicationServiceContract.DocumentParsing;
using Application.Models.DocumentParsing;
using Application.Models.DocumentParsing.Node;

namespace Infrastructure.InfrastructureShared.DocumentParser;

public static class DocumentParserExtension
{
    public static ParsedDocument ConvertToParsedDocument(this StructuredDocument structuredDocument)
    {
        var blocks = new List<ParsedBlock>();

        FlattenNodes(structuredDocument.Nodes, blocks);

        return new ParsedDocument
        {
            FullText = string.Join("\n\n", blocks.Select(b => b.Text)),
            Blocks = blocks,
            Metadata = structuredDocument.Metadata
        };
    }
    /// <summary>
    /// 平整节点
    /// </summary>
    /// <param name="nodes"></param>
    /// <param name="blocks"></param>
    private static void FlattenNodes(IEnumerable<DocumentNode> nodes, List<ParsedBlock> blocks)
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
    /// <summary>
    /// 将表格转化为文本
    /// </summary>
    /// <param name="table">表格节点</param>
    /// <returns></returns>
    private static string ConvertTableToText(TableNode table)
    {
        var lines = new List<string>();

        if (table.Headers != null)
            lines.Add(string.Join(" | ", table.Headers));

        lines.AddRange(table.Rows.Select(r => string.Join(" | ", r)));

        return string.Join("\n", lines);
    }

    /// <summary>
    /// 根据解析模式解析文档
    /// </summary>
    public static async Task<ParsedDocument> ParseWithModeAsync(
        this IDocumentParser parser,
        DocumentParseMode mode,
        Stream stream,
        string fileName,
        DocumentParseOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        switch (mode)
        {
            case DocumentParseMode.Structured:
                if (parser is IStructuredDocumentParser structuredParser)
                {
                    var structuredDoc = await structuredParser.ParseStructuredAsync(
                        stream, fileName, options, cancellationToken);
                    return structuredDoc.ConvertToParsedDocument();
                }
                break;

            case DocumentParseMode.Blocks:
                if (parser is IChunkableDocumentParser chunkableParser)
                {
                    var blocks = await chunkableParser.ParseBlocksAsync(
                        stream, fileName, options, cancellationToken);
                    return new ParsedDocument
                    {
                        Blocks = blocks.ToList(),
                        FullText = string.Join("\n\n", blocks.Select(b => b.Text))
                    };
                }
                break;
        }

        // 回退到默认解析
        return await parser.ParseAsync(stream, fileName, options, cancellationToken);
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.ApplicationServiceContract.DocumentParsing;
using Application.Models.DocumentParsing;
using Application.Models.DocumentParsing.Node;

namespace Application.ApplicationService.DocumentParsing;

/// <summary>
/// 文档解析执行器
/// 负责根据解析模式调用合适的解析方法，并处理结构化文档到解析文档的转换
/// </summary>
public class DocumentParseExecutor
{
    /// <summary>
    /// 根据解析模式解析文档
    /// </summary>
    /// <param name="parser">文档解析器</param>
    /// <param name="mode">解析模式</param>
    /// <param name="stream">文档流</param>
    /// <param name="fileName">文件名</param>
    /// <param name="options">解析选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>解析后的文档</returns>
    public async Task<ParsedDocument> ParseDocumentAsync(
        IDocumentParser parser,
        DocumentParseMode mode,
        Stream stream,
        string fileName,
        DocumentParseOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new DocumentParseOptions();

        switch (mode)
        {
            case DocumentParseMode.Structured:
                if (parser is IStructuredDocumentParser structuredParser)
                {
                    var structuredDoc = await structuredParser.ParseStructuredAsync(
                        stream, fileName, options, cancellationToken);
                    return ConvertStructuredToParsed(structuredDoc);
                }
                break;

            case DocumentParseMode.Blocks:
                if (parser is IChunkableDocumentParser chunkableParser)
                {
                    var blocks = await chunkableParser.ParseBlocksAsync(
                        stream, fileName, options, cancellationToken);
                    if (blocks != null && blocks.Any())
                    {
                        return new ParsedDocument
                        {
                            Blocks = blocks.ToList(),
                            FullText = string.Join("\n\n", blocks.Select(b => b.Text))
                        };
                    }
                }
                break;
        }

        // 回退到默认解析
        return await parser.ParseAsync(stream, fileName, options, cancellationToken);
    }

    /// <summary>
    /// 将结构化文档转换为解析文档
    /// </summary>
    /// <param name="structuredDocument">结构化文档</param>
    /// <returns>解析文档</returns>
    public ParsedDocument ConvertStructuredToParsed(StructuredDocument structuredDocument)
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
    /// 平整节点树
    /// </summary>
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

    /// <summary>
    /// 将表格转化为文本
    /// </summary>
    private string ConvertTableToText(TableNode table)
    {
        var lines = new List<string>();

        if (table.Headers != null)
            lines.Add(string.Join(" | ", table.Headers));

        lines.AddRange(table.Rows.Select(r => string.Join(" | ", r)));

        return string.Join("\n", lines);
    }
}
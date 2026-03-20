using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Application.ApplicationServiceContract.DocumentParsing;
using Application.Models.DocumentParsing;

namespace Infrastructure.InfrastructureShared.DocumentParser;

/// <summary>
/// 纯文本文档解析器。
/// </summary>
public class PlainTextDocumentParser : ChunkableDocumentParserBase
{
    /// <inheritdoc />
    protected internal override IReadOnlySet<string> SupportedExtensions { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".txt",
        ".text",
        ".log",
        ".csv",
        ".tsv",
        ".json",
        ".xml",
        ".yaml",
        ".yml",
        ".md",
        ".markdown",
        ".rst",
        ".ini",
        ".cfg",
        ".conf",
        ".config"
    };

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<ParsedBlock>> ParseBlocksCoreAsync(
        Stream stream,
        string fileName,
        DocumentParseOptions? options,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var content = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

        // 简单分块：按空行分割
        var paragraphs = content.Split(new[] { "\r\n\r\n", "\n\n", "\r\r" }, StringSplitOptions.RemoveEmptyEntries);

        var blocks = new List<ParsedBlock>();
        foreach (var paragraph in paragraphs)
        {
            if (string.IsNullOrWhiteSpace(paragraph))
                continue;

            var block = CreateTextBlock(paragraph.Trim(), BlockType.Paragraph);
            blocks.Add(block);
        }

        // 如果没有分块（例如单行文本），则将整个内容作为一个块
        if (blocks.Count == 0 && !string.IsNullOrWhiteSpace(content))
        {
            blocks.Add(CreateTextBlock(content.Trim(), BlockType.Paragraph));
        }

        return blocks;
    }

    /// <summary>
    /// 重写 CanHandle 以支持无扩展名的文件或未知扩展名。
    /// </summary>
    public override bool CanHandle(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension))
        {
            // 无扩展名文件视为纯文本
            return true;
        }

        return SupportedExtensions.Contains(extension.ToLowerInvariant());
    }
}
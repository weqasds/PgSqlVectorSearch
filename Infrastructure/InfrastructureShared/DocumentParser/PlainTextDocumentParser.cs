using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Application.ApplicationServiceContract.DocumentParsing;
using Application.Models.DocumentParsing;
using Application.Models.DocumentParsing.Node;
using DomainService.Exceptions;

namespace Infrastructure.InfrastructureShared.DocumentParser;

/// <summary>
/// 纯文本文档解析器。
/// </summary>
public class PlainTextDocumentParser : DocumentParserBase, IChunkableDocumentParser, IStructuredDocumentParser
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
    protected override async Task<ParsedDocument> ParseCoreAsync(
        Stream stream,
        string fileName,
        DocumentParseOptions? options,
        CancellationToken cancellationToken)
    {
        // 默认使用结构化解析
        var structured = await ParseStructuredCoreAsync(stream, fileName, options, cancellationToken);
        return structured.ConvertToParsedDocument();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ParsedBlock>> ParseBlocksAsync(
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
            return await ParseBlocksCoreAsync(stream, fileName, documentParseOptions, cancellationToken)
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
                $"解析文件 '{fileName}' 为块时发生错误。", ex)
            {
                FileName = fileName,
                ParserType = GetType().Name
            };
        }
    }

    /// <inheritdoc />
    protected virtual async Task<IReadOnlyList<ParsedBlock>> ParseBlocksCoreAsync(
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

            var block = ParsedBlock.CreateTextBlock(paragraph.Trim(), BlockType.Paragraph);
            blocks.Add(block);
        }

        // 如果没有分块（例如单行文本），则将整个内容作为一个块
        if (blocks.Count == 0 && !string.IsNullOrWhiteSpace(content))
        {
            blocks.Add(ParsedBlock.CreateTextBlock(content.Trim(), BlockType.Paragraph));
        }

        return blocks;
    }

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
    /// 结构化解析文档的实现。
    /// </summary>
    protected virtual async Task<StructuredDocument> ParseStructuredCoreAsync(
        Stream stream,
        string fileName,
        DocumentParseOptions? options,
        CancellationToken cancellationToken)
    {
        // 使用流式读取，避免一次性加载大文件
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var content = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

        var structuredDoc = new StructuredDocument
        {
            Metadata = new Dictionary<string, object>
            {
                ["FileName"] = fileName,
                ["CharacterCount"] = content.Length,
                ["LineCount"] = content.Split('\n').Length
            },
            Nodes = new List<DocumentNode>()
        };

        // 按空行分割文本
        var paragraphs = content.Split(new[] { "\r\n\r\n", "\n\n", "\r\r" }, StringSplitOptions.RemoveEmptyEntries);

        if (paragraphs.Length == 0 && !string.IsNullOrWhiteSpace(content))
        {
            // 单段落文档
            structuredDoc.Nodes.Add(new ParagraphNode
            {
                Text = content.Trim()
            });
        }
        else
        {
            // 多段落文档，尝试检测标题
            foreach (var paragraph in paragraphs)
            {
                if (string.IsNullOrWhiteSpace(paragraph))
                    continue;

                var trimmedParagraph = paragraph.Trim();

                // 简单标题检测逻辑
                if (IsLikelyHeading(trimmedParagraph))
                {
                    structuredDoc.Nodes.Add(new HeadingNode
                    {
                        Level = DetermineHeadingLevel(trimmedParagraph),
                        Text = trimmedParagraph
                    });
                }
                else
                {
                    structuredDoc.Nodes.Add(new ParagraphNode
                    {
                        Text = trimmedParagraph
                    });
                }
            }
        }

        return structuredDoc;
    }

    private static bool IsLikelyHeading(string text)
    {
        // 检测常见标题模式
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var lines = text.Split('\n');
        if (lines.Length > 1)
            return false; // 标题通常是单行

        var line = lines[0].Trim();

        // Markdown 风格标题
        if (line.StartsWith("# ") || line.StartsWith("## ") || line.StartsWith("### ") ||
            line.StartsWith("#### ") || line.StartsWith("##### ") || line.StartsWith("###### "))
            return true;

        // 下划线风格标题
        if (line.Length < 100 && (line.All(c => c == '=') || line.All(c => c == '-')))
            return true;

        // 短文本且全部大写（英文标题）
        if (line.Length < 50 && line == line.ToUpperInvariant() && line.Any(char.IsLetter))
            return true;

        // 以常见标题前缀开头
        var lowerLine = line.ToLowerInvariant();
        if (lowerLine.StartsWith("chapter ") || lowerLine.StartsWith("section ") ||
            lowerLine.StartsWith("part ") || lowerLine.StartsWith("appendix "))
            return true;

        return false;
    }

    private static int DetermineHeadingLevel(string headingText)
    {
        if (headingText.StartsWith("# "))
            return 1;
        if (headingText.StartsWith("## "))
            return 2;
        if (headingText.StartsWith("### "))
            return 3;
        if (headingText.StartsWith("#### "))
            return 4;
        if (headingText.StartsWith("##### "))
            return 5;
        if (headingText.StartsWith("###### "))
            return 6;

        // 默认标题级别
        return 2;
    }

    private string GetTextContent(Stream stream)
    {
        // 重置流位置以便读取
        if (stream.CanSeek)
            stream.Position = 0;

        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        return reader.ReadToEnd();
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
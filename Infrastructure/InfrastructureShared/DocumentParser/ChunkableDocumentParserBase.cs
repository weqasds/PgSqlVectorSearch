using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.ApplicationServiceContract.DocumentParsing;
using Application.Models.DocumentParsing;
using DomainService.Exceptions;

namespace Infrastructure.InfrastructureShared.DocumentParser;

/// <summary>
/// 支持分块解析的文档解析器基类。
/// </summary>
public abstract class ChunkableDocumentParserBase : DocumentParserBase, IChunkableDocumentParser
{
    /// <summary>
    /// 将文档解析为多个语义块的实现。
    /// </summary>
    protected abstract Task<IReadOnlyList<ParsedBlock>> ParseBlocksCoreAsync(
        Stream stream,
        string fileName,
        DocumentParseOptions? options,
        CancellationToken cancellationToken);

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

    /// <summary>
    /// 默认实现：先解析完整文档，然后返回其块列表。
    /// 子类可以重写此方法以提供更高效的分块解析。
    /// </summary>
    protected override async Task<ParsedDocument> ParseCoreAsync(
        Stream stream,
        string fileName,
        DocumentParseOptions? options,
        CancellationToken cancellationToken)
    {
        var blocks = await ParseBlocksAsync(stream, fileName, options, cancellationToken)
            .ConfigureAwait(false);
        var fullText = string.Join(Environment.NewLine, blocks.Select(b => b.Text));
        return new ParsedDocument
        {
            FullText = fullText,
            Blocks = blocks.ToList()
        };
    }

    /// <summary>
    /// 实用方法：创建文本块。
    /// </summary>
    protected static ParsedBlock CreateTextBlock(string text, BlockType? type = null, int? pageNumber = null)
    {
        return new ParsedBlock
        {
            Text = text,
            Type = type,
            PageNumber = pageNumber
        };
    }

    /// <summary>
    /// 实用方法：创建表格块。
    /// </summary>
    protected static ParsedBlock CreateTableBlock(string text, int? pageNumber = null)
    {
        return new ParsedBlock
        {
            Text = text,
            Type = BlockType.Table,
            PageNumber = pageNumber
        };
    }

    /// <summary>
    /// 实用方法：创建标题块。
    /// </summary>
    protected static ParsedBlock CreateTitleBlock(string text, int? pageNumber = null)
    {
        return new ParsedBlock
        {
            Text = text,
            Type = BlockType.Title,
            PageNumber = pageNumber
        };
    }
}
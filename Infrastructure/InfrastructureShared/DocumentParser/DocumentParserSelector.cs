using System;
using System.Collections.Generic;
using System.Linq;
using Application.ApplicationServiceContract.DocumentParsing;
using DomainService.Exceptions;

namespace Infrastructure.InfrastructureShared.DocumentParser;

/// <summary>
/// 文档解析器选择器，用于根据文件选择适当的解析器。
/// </summary>
public class DocumentParserSelector : IDocumentParserSelector
{
    private readonly IEnumerable<IDocumentParser> _parsers;

    /// <summary>
    /// 初始化文档解析器选择器。
    /// </summary>
    /// <param name="parsers">所有可用的文档解析器。</param>
    public DocumentParserSelector(IEnumerable<IDocumentParser> parsers)
    {
        _parsers = parsers ?? throw new ArgumentNullException(nameof(parsers));
    }

    /// <inheritdoc />
    public IDocumentParser GetParser(string fileName)
    {
        var parser = _parsers.FirstOrDefault(p => p.CanHandle(fileName));
        if (parser == null)
        {
            var supportedExtensions = _parsers
                .OfType<DocumentParserBase>()
                .SelectMany(p => p.SupportedExtensions)
                .Distinct();
            throw new DocumentFormatNotSupportedException(
                $"没有找到能够处理文件 '{fileName}' 的解析器。支持的文件扩展名: {string.Join(", ", supportedExtensions)}")
            {
                FileName = fileName,
                SupportedFormats = string.Join(", ", supportedExtensions)
            };
        }

        return parser;
    }

    /// <inheritdoc />
    public IChunkableDocumentParser? GetChunkableParser(string fileName)
    {
        var parser = _parsers.OfType<IChunkableDocumentParser>().FirstOrDefault(p => p.CanHandle(fileName));
        return parser;
    }

    /// <inheritdoc />
    public IStructuredDocumentParser? GetStructuredParser(string fileName)
    {
        var parser = _parsers.OfType<IStructuredDocumentParser>().FirstOrDefault(p => p.CanHandle(fileName));
        return parser;
    }
}


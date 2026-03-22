using System;
using System.IO;
using Application.ApplicationServiceContract.DocumentParsing;
using Application.Models.DocumentParsing;

namespace Application.ApplicationService;

/// <summary>
/// 默认文档解析策略解析器
/// </summary>
public class DocumentParseStrategyResolver : IDocumentParseStrategyResolver
{
    /// <inheritdoc />
    public DocumentParseMode Resolve(string fileName, long? fileSize = null, string? contentType = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("文件名不能为空", nameof(fileName));

        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        // 根据文件类型决定解析模式
        switch (extension)
        {
            case ".pdf":
                // PDF文档通常有结构化布局，适合结构化解析
                return DocumentParseMode.Structured;

            case ".docx":
            case ".doc":
                // Word文档可能有标题层级结构
                return DocumentParseMode.Structured;

            case ".xlsx":
            case ".xls":
                // Excel表格数据适合结构化解析
                return DocumentParseMode.Structured;

            case ".txt":
            case ".text":
            case ".md":
            case ".markdown":
                // 纯文本和Markdown文件适合块解析
                return DocumentParseMode.Blocks;

            default:
                // 默认使用块解析
                return DocumentParseMode.Blocks;
        }
    }
}
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Application.ApplicationServiceContract.DocumentParsing;
using Application.Models.DocumentParsing;
using Application.Models.DocumentParsing.Node;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DomainService.Exceptions;
using A = DocumentFormat.OpenXml.Drawing;

namespace Infrastructure.InfrastructureShared.DocumentParser;

/// <summary>
/// PowerPoint OpenXML 解析器，仅处理 .pptx。
/// </summary>
public class PowerPointDocumentParser : DocumentParserBase, IChunkableDocumentParser, IStructuredDocumentParser
{
    protected internal override IReadOnlySet<string> SupportedExtensions { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".pptx"
        };

    protected override async Task<ParsedDocument> ParseCoreAsync(
        Stream stream,
        string fileName,
        DocumentParseOptions? options,
        CancellationToken cancellationToken)
    {
        var structured = await ParseStructuredCoreAsync(stream, fileName, options, cancellationToken)
            .ConfigureAwait(false);
        return structured.ConvertToParsedDocument();
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

    protected virtual async Task<StructuredDocument> ParseStructuredCoreAsync(
        Stream stream,
        string fileName,
        DocumentParseOptions? options,
        CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            if (stream.CanSeek && stream.Position != 0)
            {
                stream.Position = 0;
            }

            using var presentation = PresentationDocument.Open(stream, false);
            var presentationPart = presentation.PresentationPart;
            if (presentationPart?.Presentation?.SlideIdList == null)
            {
                return new StructuredDocument
                {
                    Metadata = new Dictionary<string, object>
                    {
                        ["FileName"] = fileName,
                        ["SlideCount"] = 0
                    }
                };
            }

            var nodes = new List<DocumentNode>();
            var slideIndex = 0;

            foreach (var slideId in presentationPart.Presentation.SlideIdList.Elements<SlideId>())
            {
                cancellationToken.ThrowIfCancellationRequested();
                slideIndex++;

                var slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);
                var texts = ExtractSlideTexts(slidePart).ToList();
                if (texts.Count == 0)
                {
                    continue;
                }

                var titleText = texts.First();
                var slideHeading = new HeadingNode
                {
                    Text = titleText,
                    Level = 2,
                    PageNumber = slideIndex,
                    Metadata = new Dictionary<string, object>
                    {
                        ["SlideNumber"] = slideIndex
                    }
                };

                foreach (var bodyText in texts.Skip(1))
                {
                    if (string.IsNullOrWhiteSpace(bodyText))
                    {
                        continue;
                    }

                    slideHeading.Children.Add(new ParagraphNode
                    {
                        Text = bodyText,
                        PageNumber = slideIndex
                    });
                }

                nodes.Add(slideHeading);
            }

            return new StructuredDocument
            {
                Title = Path.GetFileNameWithoutExtension(fileName),
                Nodes = nodes,
                Metadata = new Dictionary<string, object>
                {
                    ["FileName"] = fileName,
                    ["SlideCount"] = slideIndex
                }
            };
        }, cancellationToken).ConfigureAwait(false);
    }

    private static IEnumerable<string> ExtractSlideTexts(SlidePart slidePart)
    {
        return (slidePart.Slide?
            .Descendants<A.Text>()
            .Select(text => text.Text?.Trim())
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Cast<string>()) ?? Enumerable.Empty<string>();
    }
}

using Application.Models.DocumentParsing;

namespace Application.ApplicationServiceContract.DocumentParsing;
public interface IStructuredDocumentParser : IDocumentParser
{
    Task<StructuredDocument> ParseStructuredAsync(
        Stream stream,
        string fileName,
        DocumentParseOptions? documentParseOptions = null,
        CancellationToken cancellationToken = default);
}
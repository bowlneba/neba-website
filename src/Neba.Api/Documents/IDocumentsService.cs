
namespace Neba.Api.Documents;

internal interface IDocumentsService
{
    Task<DocumentDto?> GetDocumentAsHtmlAsync(string documentName, CancellationToken cancellationToken);
}
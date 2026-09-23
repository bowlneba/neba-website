using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Documents.RefreshDocument;

namespace Neba.Api.Features.Documents.RefreshDocument;

internal sealed class RefreshDocumentRequestValidator
    : Validator<RefreshDocumentRequest>
{
    public RefreshDocumentRequestValidator()
    {
        RuleFor(r => r.DocumentName)
            .NotEmpty()
            .WithErrorCode("RefreshDocumentRequest.DocumentNameRequired")
            .WithMessage("DocumentName is required.")
            .MaximumLength(100)
            .WithErrorCode("RefreshDocumentRequest.DocumentNameTooLong")
            .WithMessage("DocumentName must be 100 characters or fewer.");
    }
}
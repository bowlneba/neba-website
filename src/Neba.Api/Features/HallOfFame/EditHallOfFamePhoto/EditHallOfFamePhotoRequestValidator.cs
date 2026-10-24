using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.HallOfFame.EditHallOfFamePhoto;
using Neba.Api.Features.HallOfFame.Domain;

namespace Neba.Api.Features.HallOfFame.EditHallOfFamePhoto;

internal sealed class EditHallOfFamePhotoRequestValidator
    : Validator<EditHallOfFamePhotoRequest>
{
    public EditHallOfFamePhotoRequestValidator()
    {
        RuleFor(r => r.Id)
            .NotEmpty().WithErrorCode("EditHallOfFamePhotoRequest.IdRequired").WithMessage("Id is required.")
            .Length(26).WithErrorCode("EditHallOfFamePhotoRequest.IdInvalidLength").WithMessage("Id must be a 26-character ULID.");

        RuleFor(r => r.Input)
            .NotNull().WithErrorCode("EditHallOfFamePhotoRequest.InputRequired").WithMessage("Input is required.");

        When(r => r.Input is not null, () =>
        {
            RuleFor(r => r.Input.FocalX)
                .InclusiveBetween(PhotoFocalPoint.Min, PhotoFocalPoint.Max)
                .WithErrorCode("EditHallOfFamePhotoRequest.FocalXOutOfRange").WithMessage("FocalX must be between 0 and 100.")
                .When(r => r.Input.FocalX.HasValue);

            RuleFor(r => r.Input.FocalY)
                .InclusiveBetween(PhotoFocalPoint.Min, PhotoFocalPoint.Max)
                .WithErrorCode("EditHallOfFamePhotoRequest.FocalYOutOfRange").WithMessage("FocalY must be between 0 and 100.")
                .When(r => r.Input.FocalY.HasValue);

            RuleFor(r => r.Input.Zoom)
                .InclusiveBetween(PhotoFocalPoint.MinZoom, PhotoFocalPoint.MaxZoom)
                .WithErrorCode("EditHallOfFamePhotoRequest.ZoomOutOfRange").WithMessage("Zoom must be between 100 and 300.")
                .When(r => r.Input.Zoom.HasValue);

            RuleFor(r => r.Input)
                .Must(input => input.FocalX.HasValue == input.FocalY.HasValue && input.FocalY.HasValue == input.Zoom.HasValue)
                .WithErrorCode("EditHallOfFamePhotoRequest.FramingIncomplete").WithMessage("FocalX, FocalY, and Zoom must all be set or all be omitted.");
        });

        When(r => r.Input?.Photo is not null, () =>
        {
            RuleFor(r => r.Input.Photo!.Container)
                .NotEmpty().WithErrorCode("EditHallOfFamePhotoRequest.PhotoContainerRequired").WithMessage("Photo container is required.");

            RuleFor(r => r.Input.Photo!.Path)
                .NotEmpty().WithErrorCode("EditHallOfFamePhotoRequest.PhotoPathRequired").WithMessage("Photo path is required.");

            RuleFor(r => r.Input.Photo!.ContentType)
                .NotEmpty().WithErrorCode("EditHallOfFamePhotoRequest.PhotoContentTypeRequired").WithMessage("Photo content type is required.");

            RuleFor(r => r.Input.Photo!.SizeInBytes)
                .GreaterThan(0).WithErrorCode("EditHallOfFamePhotoRequest.PhotoSizeInvalid").WithMessage("Photo size must be greater than zero.");
        });
    }
}

using Neba.Api.Features.Tournaments.CreateOilPattern;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.OilPatterns;

namespace Neba.Api.Tests.Features.Tournaments.CreateOilPattern;

[UnitTest]
[Component("Tournaments")]
public sealed class CreateOilPatternRequestValidatorTests
{
    private readonly CreateOilPatternRequestValidator _validator = new();

    [Fact(DisplayName = "Validate should succeed when the request is valid")]
    public void Validate_ShouldSucceed_WhenRequestIsValid()
    {
        var result = _validator.Validate(CreateOilPatternRequestFactory.Create());

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Validate should fail with NameRequired error when Name is empty")]
    public void Validate_ShouldFail_WhenNameIsEmpty()
    {
        var result = _validator.Validate(CreateOilPatternRequestFactory.Create(name: ""));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == "OilPattern.Name"
            && e.ErrorCode == "CreateOilPatternRequest.NameRequired");
    }

    [Fact(DisplayName = "Validate should fail with NameTooLong error when Name exceeds 63 characters")]
    public void Validate_ShouldFail_WhenNameExceedsMaxLength()
    {
        var result = _validator.Validate(CreateOilPatternRequestFactory.Create(name: new string('a', 64)));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == "OilPattern.Name"
            && e.ErrorCode == "CreateOilPatternRequest.NameTooLong");
    }

    [Fact(DisplayName = "Validate should fail with LengthMustBePositive error when Length is zero")]
    public void Validate_ShouldFail_WhenLengthIsZero()
    {
        var result = _validator.Validate(CreateOilPatternRequestFactory.Create(length: 0));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == "OilPattern.Length"
            && e.ErrorCode == "CreateOilPatternRequest.LengthMustBePositive");
    }

    [Fact(DisplayName = "Validate should fail with VolumeMustBePositive error when Volume is zero")]
    public void Validate_ShouldFail_WhenVolumeIsZero()
    {
        var result = _validator.Validate(CreateOilPatternRequestFactory.Create(volume: 0m));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == "OilPattern.Volume"
            && e.ErrorCode == "CreateOilPatternRequest.VolumeMustBePositive");
    }

    [Fact(DisplayName = "Validate should fail with LeftRatioInvalid error when LeftRatio is negative")]
    public void Validate_ShouldFail_WhenLeftRatioIsNegative()
    {
        var result = _validator.Validate(CreateOilPatternRequestFactory.Create(leftRatio: -1m));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == "OilPattern.LeftRatio"
            && e.ErrorCode == "CreateOilPatternRequest.LeftRatioInvalid");
    }

    [Fact(DisplayName = "Validate should fail with RightRatioInvalid error when RightRatio is negative")]
    public void Validate_ShouldFail_WhenRightRatioIsNegative()
    {
        var result = _validator.Validate(CreateOilPatternRequestFactory.Create(rightRatio: -1m));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == "OilPattern.RightRatio"
            && e.ErrorCode == "CreateOilPatternRequest.RightRatioInvalid");
    }
}
using Neba.Api.Contracts.Sponsors.EditSponsor;

namespace Neba.TestFactory.Sponsors;

public static class EditSponsorRequestFactory
{
    public const string ValidId = "01KNPMEYKAR8YHHZ0FSPX91MNN";

    public static EditSponsorRequest Create(
        string? id = null,
        EditSponsorInput? sponsor = null)
        => new()
        {
            Id = id ?? ValidId,
            Sponsor = sponsor ?? EditSponsorInputFactory.Create()
        };
}

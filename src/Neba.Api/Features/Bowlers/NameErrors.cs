using ErrorOr;

namespace Neba.Api.Features.Bowlers;

internal static class NameErrors
{
    public static Error FirstNameRequired
        => Error.Validation(
            code: "Name.FirstNameRequired",
            description: "First name is required."
        );

    public static Error LastNameRequired
        => Error.Validation(
            code: "Name.LastNameRequired",
            description: "Last name is required."
        );
}
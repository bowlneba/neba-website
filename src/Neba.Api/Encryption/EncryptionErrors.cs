using ErrorOr;

namespace Neba.Api.Encryption;

internal static class EncryptionErrors
{
    // One error for every cause, so a caller cannot learn which step failed.
    public static Error InvalidClientValue
        => Error.Validation("Encryption.InvalidValue", "The submitted value could not be read.");
}
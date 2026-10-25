using Bogus;

using Neba.Api.Contracts.Security.ChangePassword;

namespace Neba.TestFactory.Security;

public static class ChangePasswordRequestFactory
{
    public const string ValidCurrentPassword = "OldPassword1";
    public const string ValidNewPassword = "NewPassword1";

    public static ChangePasswordRequest Create(
        string? currentPassword = null,
        string? newPassword = null)
        => new()
        {
            CurrentPassword = currentPassword ?? ValidCurrentPassword,
            NewPassword = newPassword ?? ValidNewPassword
        };

    internal static IReadOnlyCollection<ChangePasswordRequest> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ => new ChangePasswordRequest
        {
            CurrentPassword = faker.Internet.Password(12) + "1",
            NewPassword = faker.Internet.Password(12) + "2"
        })];
    }

    public static IReadOnlyCollection<ChangePasswordRequest> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}

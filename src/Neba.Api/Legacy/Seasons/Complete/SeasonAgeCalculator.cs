namespace Neba.Api.Legacy.Seasons.Complete;

internal static class SeasonAgeCalculator
{
    public static int? AgeOnDate(DateOnly? dateOfBirth, DateOnly asOf)
    {
        if (dateOfBirth is not { } dob)
        {
            return null;
        }

        var age = asOf.Year - dob.Year;
        if (dob > asOf.AddYears(-age))
        {
            age--;
        }

        return age;
    }
}

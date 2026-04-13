namespace Neba.Website.Server.Stats;

/// <summary>
/// View model representing the finals per entry for a bowler in the stats page. This is used to calculate the ratio of finals appearances to total entries for each bowler, which can be used to assess their performance in tournaments. The rank is determined based on the highest finals per entry ratio, with 1 being the highest. The bowler's name and ID are included for display and linking purposes in the stats table. If a bowler has not participated in any entries, the finals per entry ratio will be 0 to avoid division by zero errors.
/// </summary>
public sealed record FinalsPerEntryRowViewModel
{
    /// <summary>
    /// The rank of the bowler based on finals per entry, with 1 being the highest. This is calculated by sorting the bowlers in descending order of their finals per entry ratio and assigning ranks accordingly. Bowlers with the same finals per entry ratio will receive the same rank, and the next rank will be skipped to maintain the correct ranking order. For example, if two bowlers are tied for first place, both will receive a rank of 1, and the next bowler will receive a rank of 3.
    /// </summary>
    public required int Rank { get; init; }

    /// <summary>
    /// The unique identifier of the bowler, represented as an Ulid. This is used to link to the bowler's profile page in the stats table, allowing users to view more detailed information about the bowler's performance and history. The BowlerId is essential for maintaining the connection between the stats data and the corresponding bowler's profile, ensuring that users can easily navigate to the relevant information when viewing the stats page.
    /// </summary>
    public required Ulid BowlerId { get; init; }

    /// <summary>
    /// The name of the bowler. This is displayed in the stats table and linked to the bowler's profile page using the BowlerId. The BowlerName provides a user-friendly way to identify the bowler in the stats table, allowing users to quickly recognize and compare bowlers based on their names while also providing a direct link to their profiles for more detailed information.
    /// </summary>
    public required string BowlerName { get; init; }

    /// <summary>
    /// The total number of finals appearances made by the bowler across all tournaments. This is used to calculate the finals per entry ratio and is displayed in the stats table. The Finals count reflects the bowler's success in reaching the final stages of tournaments, which is an important metric for assessing their performance and consistency in competitive play. A higher number of finals appearances indicates a stronger performance in tournaments, contributing to a higher finals per entry ratio.
    /// </summary>
    public required int Finals { get; init; }

    /// <summary>
    /// The total number of entries made by the bowler across all tournaments. This is used to calculate the finals per entry ratio and is displayed in the stats table. The Entries count represents the bowler's level of participation in tournaments, which is crucial for understanding their overall performance. A higher number of entries indicates more opportunities for the bowler to reach the finals, and it is essential for calculating the finals per entry ratio accurately. If a bowler has not participated in any entries, this value will be 0, which will result in a finals per entry ratio of 0 to avoid division by zero errors when calculating the ratio.
    /// </summary>
    public required int Entries { get; init; }

    /// <summary>
    /// The average number of finals appearances per entry for the bowler. This is calculated by dividing the total number of finals appearances (Finals) by the total number of entries (Entries). If the bowler has not participated in any entries (Entries is 0), this value will be 0 to avoid division by zero errors. The FinalsPerEntry ratio provides insight into the bowler's performance in tournaments, indicating how often they reach the finals relative to their level of participation. A higher finals per entry ratio suggests a stronger performance and consistency in reaching the final stages of tournaments, which can be a key factor in ranking bowlers in the stats page.
    /// </summary>
    public decimal FinalsPerEntry
        => Entries == 0 ? 0 : (decimal)Finals / Entries;
}
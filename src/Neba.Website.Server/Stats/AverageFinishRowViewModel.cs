namespace Neba.Website.Server.Stats;

/// <summary>
/// View model representing the average finish for a bowler in the stats page. This is used to calculate the average finishing position of a bowler in tournaments, which can be an important metric for assessing their performance and consistency. The rank is determined based on the lowest average finish, with 1 being the best. The bowler's name and ID are included for display and linking purposes in the stats table. The AverageFinish is calculated by taking into account the finishing positions of the bowler in all tournaments they have participated in, with lower values indicating better performance. The Finals count represents the number of times the bowler has reached the finals, which can also be an indicator of their success in tournaments. The Winnings represent the total prize money earned by the bowler across all tournaments, which can be used to further assess their performance and success in competitive play.
/// </summary>
public sealed record AverageFinishRowViewModel
{
    /// <summary>
    /// The rank of the bowler based on average finish, with 1 being the best. This is calculated by sorting the bowlers in ascending order of their average finish, with lower values indicating better performance. Bowlers with the same average finish will receive the same rank, and the next rank will be skipped to maintain the correct ranking order. For example, if two bowlers are tied for first place with an average finish of 2.5, both will receive a rank of 1, and the next bowler with an average finish of 3.0 will receive a rank of 3. This ranking system allows for a clear comparison of bowlers based on their average finishing positions in tournaments.
    /// </summary>
    public required int Rank { get; init; }

    /// <summary>
    /// The unique identifier of the bowler, represented as an Ulid. This is used to link to the bowler's profile page in the stats table, allowing users to view more detailed information about the bowler's performance and history. The BowlerId is essential for maintaining the connection between the stats data and the corresponding bowler's profile, ensuring that users can easily navigate to the relevant information when viewing the stats page.
    /// </summary>
    public required string BowlerId { get; init; }

    /// <summary>
    /// The name of the bowler. This is displayed in the stats table and linked to the bowler's profile page using the BowlerId. The BowlerName provides a user-friendly way to identify the bowler in the stats table, allowing users to quickly recognize and compare bowlers based on their names while also providing a direct link to their profiles for more detailed information.
    /// </summary>
    public required string BowlerName { get; init; }

    /// <summary>
    /// The average finishing position of the bowler in tournaments. This is calculated by taking into account the finishing positions of the bowler in all tournaments they have participated in, with lower values indicating better performance. The AverageFinish is an important metric for assessing a bowler's consistency and success in tournaments, as it reflects how well they perform on average across multiple events. A lower average finish suggests that the bowler consistently finishes in higher positions, which can contribute to a higher rank in the stats page. The AverageFinish is displayed in the stats table and used for ranking the bowlers based on their performance in tournaments.
    /// </summary>
    public required decimal AverageFinish { get; init; }

    /// <summary>
    /// The total number of finals appearances made by the bowler across all tournaments. This is used to provide additional context about the bowler's performance in tournaments, as reaching the finals is often an indicator of success and consistency. The Finals count reflects the bowler's ability to perform well in competitive play and can be an important factor in assessing their overall performance. A higher number of finals appearances indicates a stronger performance in tournaments, contributing to a better understanding of the bowler's achievements and consistency in reaching the final stages of tournaments. The Finals count is displayed in the stats table alongside the average finish and winnings to provide a comprehensive view of the bowler's performance.
    /// </summary>
    public required int Finals { get; init; }

    /// <summary>
    /// The total prize money earned by the bowler across all tournaments. This is used to further assess the bowler's performance and success in competitive play, as higher winnings often indicate better performance in tournaments. The Winnings represent the financial rewards earned by the bowler based on their finishing positions in tournaments, with higher placements typically resulting in larger prize money. The Winnings are displayed in the stats table alongside the average finish and finals count to provide a comprehensive view of the bowler's achievements and success in tournaments. A higher total winnings amount can be an indicator of a bowler's ability to consistently perform well and earn significant rewards in competitive play.
    /// </summary>
    public required decimal Winnings { get; init; }
}
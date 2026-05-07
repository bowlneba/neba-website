using Ardalis.SmartEnum;

namespace Neba.Domain;

/// <summary>
/// Logical operators for combining Criteria within a Side Cut Criterion Group (Group Operator)
/// and for combining Criterion Groups within a Side Cut (Group Composition Operator).
/// </summary>
public sealed class LogicalOperator
    : SmartEnum<LogicalOperator, string>
{
    /// <summary>
    /// All conditions in the group must be satisfied.
    /// </summary>
    public static readonly LogicalOperator And = new(nameof(And), "AND");

    /// <summary>
    /// At least one condition in the group must be satisfied.
    /// </summary>
    public static readonly LogicalOperator Or = new(nameof(Or), "OR");

    /// <summary>
    /// Negates a condition. Not used in Side Cut eligibility evaluation — reserved for future use.
    /// </summary>
    public static readonly LogicalOperator Not = new(nameof(Not), "NOT");

    private LogicalOperator(string name, string value)
        : base(name, value)
    { }
}
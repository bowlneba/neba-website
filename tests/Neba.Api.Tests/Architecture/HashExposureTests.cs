using System.Reflection;
using System.Text.RegularExpressions;

using FastEndpoints;

using Neba.Api.Contracts;
using Neba.Api.Database;
using Neba.Api.Encryption;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Architecture;

/// <summary>
/// An SSN hash must never leave the API.
/// </summary>
[UnitTest]
[Component("Architecture")]
public sealed partial class HashExposureTests
{
    [GeneratedRegex("(ssn|socialsecurity).*hash|hash.*(ssn|socialsecurity)", RegexOptions.IgnoreCase)]
    private static partial Regex SsnHashName();

    // The given types and everything reachable from their public properties.
    private static List<string> FindHashProperties(IEnumerable<Type> roots)
    {
        var offenders = new List<string>();
        var seen = new HashSet<Type>();
        var queue = new Queue<Type>(roots);

        while (queue.TryDequeue(out var type))
        {
            if (!seen.Add(type))
            {
                continue;
            }

            foreach (var argument in type.IsGenericType ? type.GetGenericArguments() : [])
            {
                queue.Enqueue(argument);
            }

            if (type.IsArray)
            {
                queue.Enqueue(type.GetElementType()!);
            }

            if (type.Namespace?.StartsWith("Neba.", StringComparison.Ordinal) != true)
            {
                continue;
            }

            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public))
            {
                var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                var isHashType = propertyType.IsGenericType
                    && (propertyType.GetGenericTypeDefinition() == typeof(Hashed<>)
                        || propertyType.GetGenericTypeDefinition() == typeof(EncryptedValue<>));

                if (isHashType || SsnHashName().IsMatch(property.Name))
                {
                    offenders.Add($"{type.FullName}.{property.Name}");
                }

                queue.Enqueue(propertyType);
            }
        }

        return offenders;
    }

    private static IEnumerable<Type> EndpointResponseTypes()
    {
        foreach (var type in typeof(AppDbContext).Assembly.GetTypes().Where(t => !t.IsAbstract))
        {
            for (var parent = type.BaseType; parent is not null; parent = parent.BaseType)
            {
                if (!parent.IsGenericType)
                {
                    continue;
                }

                var definition = parent.GetGenericTypeDefinition();
                if (definition == typeof(Endpoint<,>))
                {
                    yield return parent.GetGenericArguments()[1];
                }
                else if (definition == typeof(EndpointWithoutRequest<>))
                {
                    yield return parent.GetGenericArguments()[0];
                }
            }
        }
    }

    [Fact(DisplayName = "No contract type should have an SSN hash property")]
    public void Contracts_ShouldNotExposeAnSsnHash()
    {
        // Arrange
        var types = typeof(CollectionResponse<>).Assembly.GetTypes();

        // Act
        var offenders = FindHashProperties(types);

        // Assert
        offenders.ShouldBeEmpty();
    }

    [Fact(DisplayName = "No endpoint response should have an SSN hash property")]
    public void EndpointResponses_ShouldNotExposeAnSsnHash()
    {
        // Arrange
        var types = EndpointResponseTypes().ToList();

        // Act
        var offenders = FindHashProperties(types);

        // Assert
        types.ShouldNotBeEmpty();
        offenders.ShouldBeEmpty();
    }

    [Fact(DisplayName = "The check should catch a hash property by type or by name, however deeply nested")]
    public void FindHashProperties_ShouldReportOffenders()
    {
        // Arrange
        Type[] types = [typeof(Outer)];

        // Act
        var offenders = FindHashProperties(types);

        // Assert
        offenders.Count.ShouldBe(2);
        offenders.ShouldContain(o => o.EndsWith(".Fingerprint", StringComparison.Ordinal));
        offenders.ShouldContain(o => o.EndsWith(".SsnHash", StringComparison.Ordinal));
    }

    // Properties here are public so the check, which reads public properties, can see them.
    internal sealed class Outer
    {
        public IReadOnlyCollection<Inner> Items { get; init; } = [];
    }

    internal sealed class Inner
    {
        public Hashed<BowlerPii>? Fingerprint { get; init; }

        public string? SsnHash { get; init; }
    }
}
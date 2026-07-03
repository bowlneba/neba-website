using Microsoft.Extensions.Compliance.Redaction;

namespace Neba.Api.Compliance;

/// <summary>Keeps the first character of the value and replaces the remainder with '*'.</summary>
internal sealed class StarMaskingRedactor : Redactor
{
    public override int GetRedactedLength(ReadOnlySpan<char> input)
        => input.Length;

    public override int Redact(ReadOnlySpan<char> source, Span<char> destination)
    {
        if (source.IsEmpty)
        {
            return 0;
        }

        destination[0] = source[0];
        destination[1..source.Length].Fill('*');

        return source.Length;
    }
}
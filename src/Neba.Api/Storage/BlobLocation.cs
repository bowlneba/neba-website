namespace Neba.Api.Storage;

/// <summary>
/// Container and path of a blob, logged as structured tags through <c>[LogProperties]</c>.
/// </summary>
/// <param name="Container">The blob container name.</param>
/// <param name="Path">The blob path inside the container.</param>
internal readonly record struct BlobLocation(string Container, string Path)
{
    /// <inheritdoc />
    public override string ToString() => $"{Container}/{Path}";
}
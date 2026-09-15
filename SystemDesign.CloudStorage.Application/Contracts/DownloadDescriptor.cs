namespace SystemDesign.CloudStorage.Application.Contracts;

/// <summary>
/// A downloadable object version descriptor with a stream factory.
/// </summary>
public sealed record DownloadDescriptor(
    Guid VersionId,
    string ETag,
    string ContentType,
    long Length,
    string Sha256,
    IReadOnlyDictionary<string, string> Metadata,
    Func<CancellationToken, Task<Stream>> OpenReadAsync);

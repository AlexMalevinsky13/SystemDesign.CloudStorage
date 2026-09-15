namespace SystemDesign.CloudStorage.Application.Contracts;

/// <summary>
/// An object listing item.
/// </summary>
public sealed record ObjectListItem(
    string Key,
    Guid VersionId,
    long ContentLength,
    string ETag,
    DateTimeOffset CreatedAtUtc);

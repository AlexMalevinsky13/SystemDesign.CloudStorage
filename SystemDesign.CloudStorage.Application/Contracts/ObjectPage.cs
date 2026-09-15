namespace SystemDesign.CloudStorage.Application.Contracts;

/// <summary>
/// A page of object listing results.
/// </summary>
public sealed record ObjectPage(IReadOnlyList<ObjectListItem> Items, string? ContinuationToken);

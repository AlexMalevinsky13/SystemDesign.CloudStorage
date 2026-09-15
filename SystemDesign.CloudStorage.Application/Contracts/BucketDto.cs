namespace SystemDesign.CloudStorage.Application.Contracts;

/// <summary>
/// A bucket response.
/// </summary>
public sealed record BucketDto(string Name, DateTimeOffset CreatedAtUtc);

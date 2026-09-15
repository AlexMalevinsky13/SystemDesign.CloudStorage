namespace SystemDesign.CloudStorage.Application.Contracts;

/// <summary>
/// A bucket creation request.
/// </summary>
public sealed record CreateBucketRequest(string Name);

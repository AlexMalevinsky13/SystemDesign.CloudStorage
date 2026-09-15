namespace SystemDesign.CloudStorage.Application.Contracts;

/// <summary>
/// An upload result.
/// </summary>
public sealed record UploadResult(Guid VersionId, string ETag, long ContentLength, string Sha256);

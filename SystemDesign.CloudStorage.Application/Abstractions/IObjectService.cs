using SystemDesign.CloudStorage.Application.Contracts;

namespace SystemDesign.CloudStorage.Application.Abstractions;

/// <summary>
/// Provides streaming object operations.
/// </summary>
public interface IObjectService
{
    /// <summary>
    /// Streams a new object version into storage.
    /// </summary>
    Task<UploadResult> UploadAsync(
        Guid ownerId,
        string bucket,
        string key,
        Stream content,
        long? declaredLength,
        string? contentType,
        string? fileName,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets version metadata without reading the blob.
    /// </summary>
    Task<DownloadDescriptor> GetAsync(
        Guid ownerId,
        string bucket,
        string key,
        Guid? versionId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Creates a delete marker or removes a specific version.
    /// </summary>
    Task DeleteAsync(Guid ownerId, string bucket, string key, Guid? versionId, CancellationToken cancellationToken);

    /// <summary>
    /// Lists current objects using database-level pagination.
    /// </summary>
    Task<ObjectPage> ListAsync(
        Guid ownerId,
        string bucket,
        string? prefix,
        string? cursor,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Lists object versions.
    /// </summary>
    Task<IReadOnlyList<ObjectVersionDto>> ListVersionsAsync(
        Guid ownerId,
        string bucket,
        string key,
        CancellationToken cancellationToken);
}
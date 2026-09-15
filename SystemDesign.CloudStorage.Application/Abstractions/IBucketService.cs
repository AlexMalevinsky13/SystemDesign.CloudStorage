using SystemDesign.CloudStorage.Application.Contracts;

namespace SystemDesign.CloudStorage.Application.Abstractions;

/// <summary>
/// Provides bucket operations.
/// </summary>
public interface IBucketService
{
    /// <summary>
    /// Creates a bucket.
    /// </summary>
    Task<BucketDto> CreateAsync(Guid ownerId, string name, CancellationToken cancellationToken);

    /// <summary>
    /// Lists the user buckets.
    /// </summary>
    Task<IReadOnlyList<BucketDto>> ListAsync(Guid ownerId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a bucket.
    /// </summary>
    Task<BucketDto> GetAsync(Guid ownerId, string name, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an empty bucket.
    /// </summary>
    Task DeleteAsync(Guid ownerId, string name, CancellationToken cancellationToken);
}

using SystemDesign.CloudStorage.Application.Contracts;

namespace SystemDesign.CloudStorage.Application.Abstractions;

/// <summary>
/// Provides multipart upload operations.
/// </summary>
public interface IMultipartService
{
    /// <summary>
    /// Starts a multipart upload.
    /// </summary>
    Task<Guid> InitiateAsync(
        Guid ownerId,
        string bucket,
        string key,
        string? contentType,
        CancellationToken cancellationToken);

    /// <summary>
    /// Streams and stores a multipart part.
    /// </summary>
    Task<string> UploadPartAsync(
        Guid ownerId,
        Guid uploadId,
        int partNumber,
        Stream content,
        long? length,
        CancellationToken cancellationToken);

    /// <summary>
    /// Completes an upload by streaming its parts.
    /// </summary>
    Task<UploadResult> CompleteAsync(Guid ownerId, Guid uploadId, CancellationToken cancellationToken);

    /// <summary>
    /// Aborts a multipart upload.
    /// </summary>
    Task AbortAsync(Guid ownerId, Guid uploadId, CancellationToken cancellationToken);
}

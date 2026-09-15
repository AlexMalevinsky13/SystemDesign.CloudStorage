using Microsoft.AspNetCore.Mvc;
using SystemDesign.CloudStorage.Application.Abstractions;
using SystemDesign.CloudStorage.Application.Contracts;

namespace SystemDesign.CloudStorage.Api.Controllers;

/// <summary>
/// Multipart HTTP lifecycle.
/// </summary>
[Route("api/buckets/{bucketName}/multipart")]
public sealed class MultipartController(IMultipartService service) : OwnedController
{
    /// <summary>
    /// Starts a multipart upload.
    /// </summary>
    [HttpPost]
    public async Task<object> Initiate(string bucketName, [FromQuery] string key, [FromQuery] string? contentType, CancellationToken ct) =>
        new { uploadId = await service.InitiateAsync(OwnerId, bucketName, key, contentType, ct) };

    /// <summary>
    /// Uploads a multipart part.
    /// </summary>
    [HttpPut("{uploadId:guid}/parts/{partNumber:int}")]
    public async Task<object> Part(string bucketName, Guid uploadId, int partNumber, CancellationToken ct) =>
        new
        {
            etag = await service.UploadPartAsync(OwnerId, uploadId, partNumber, Request.Body, Request.ContentLength, ct)
        };

    /// <summary>
    /// Completes a multipart upload.
    /// </summary>
    [HttpPost("{uploadId:guid}/complete")]
    public Task<UploadResult> Complete(string bucketName, Guid uploadId, CancellationToken ct) => 
        service.CompleteAsync(OwnerId, uploadId, ct);

    /// <summary>
    /// Aborts a multipart upload.
    /// </summary>
    [HttpDelete("{uploadId:guid}")]
    public async Task<IActionResult> Abort(string bucketName, Guid uploadId, CancellationToken ct)
    {
        await service.AbortAsync(OwnerId, uploadId, ct);
        return NoContent();
    }
}

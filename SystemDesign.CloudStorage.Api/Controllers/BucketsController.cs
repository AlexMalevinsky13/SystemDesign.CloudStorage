using Microsoft.AspNetCore.Mvc;
using SystemDesign.CloudStorage.Application.Abstractions;
using SystemDesign.CloudStorage.Application.Contracts;

namespace SystemDesign.CloudStorage.Api.Controllers;

/// <summary>
/// HTTP endpoints buckets.
/// </summary>
[Route("api/buckets")]
public sealed class BucketsController(IBucketService service) : OwnedController
{
    /// <summary>
    /// Creates a bucket.
    /// </summary>
    [HttpPost]
    public Task<BucketDto> Create(CreateBucketRequest r, CancellationToken ct) => 
        service.CreateAsync(OwnerId, r.Name, ct);

    /// <summary>
    /// Lists buckets.
    /// </summary>
    [HttpGet]
    public Task<IReadOnlyList<BucketDto>> List(CancellationToken ct) => 
        service.ListAsync(OwnerId, ct);

    /// <summary>
    /// Gets a bucket.
    /// </summary>
    [HttpGet("{bucketName}")]
    public Task<BucketDto> Get(string bucketName, CancellationToken ct) => 
        service.GetAsync(OwnerId, bucketName, ct);

    /// <summary>
    /// Deletes an empty bucket.
    /// </summary>
    [HttpDelete("{bucketName}")]
    public async Task<IActionResult> Delete(string bucketName, CancellationToken ct)
    {
        await service.DeleteAsync(OwnerId, bucketName, ct);
        return NoContent();
    }
}
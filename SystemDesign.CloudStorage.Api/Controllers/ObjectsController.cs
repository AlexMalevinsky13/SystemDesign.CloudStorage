using Microsoft.AspNetCore.Mvc;
using SystemDesign.CloudStorage.Application.Abstractions;
using SystemDesign.CloudStorage.Application.Contracts;
using SystemDesign.CloudStorage.Application.Exceptions;

namespace SystemDesign.CloudStorage.Api.Controllers;

/// <summary>
/// Provides streaming object HTTP endpoints.
/// </summary>
[Route("api/buckets/{bucketName}/objects")]
public sealed class ObjectsController(IObjectService service) : OwnedController
{
    /// <summary>
    /// Lists objects without materializing the entire bucket.
    /// </summary>
    [HttpGet]
    public Task<ObjectPage> List(string bucketName,
                                [FromQuery] string? prefix,
                                [FromQuery] string? continuationToken,
                                [FromQuery] int pageSize = 100,
                                CancellationToken ct = default) =>
        service.ListAsync(OwnerId, bucketName, prefix, continuationToken, pageSize, ct);

    /// <summary>
    /// Lists versions for an object key.
    /// </summary>
    [HttpGet("versions")]
    public Task<IReadOnlyList<ObjectVersionDto>> Versions(string bucketName, [FromQuery] string key, CancellationToken ct) => 
        service.ListVersionsAsync(OwnerId, bucketName, key, ct);

    /// <summary>
    /// Uploads the request body without buffering it in memory.
    /// </summary>
    [HttpPut("{*key}")]
    public async Task<ActionResult<UploadResult>> Put(string bucketName, string key, CancellationToken ct)
    {
        var metadata = Request.Headers.Where(x => x.Key.StartsWith("x-meta-", StringComparison.OrdinalIgnoreCase))
                           .ToDictionary(x => x.Key[7..], x => x.Value.ToString());

        var result = await service.UploadAsync(OwnerId,
            bucketName,
            key,
            Request.Body,
            Request.ContentLength,
            Request.ContentType,
            Request.Headers["x-file-name"],
            metadata,
            ct);

        Response.Headers.ETag = result.ETag;

        return Ok(result);
    }

    /// <summary>
    /// Downloads an object or a single byte range.
    /// </summary>
    [HttpGet("{*key}")]
    public async Task<IActionResult> Get(string bucketName, string key, [FromQuery] Guid? versionId, CancellationToken ct)
    {
        var d = await service.GetAsync(OwnerId, bucketName, key, versionId, ct);
        
        ApplyHeaders(d);
        CheckPreconditions(d.ETag);
        
        var range = ParseRange(Request.Headers.Range, d.Length);
        if (range is null)
        {
            var stream = await d.OpenReadAsync(ct);
            return File(stream, d.ContentType, enableRangeProcessing: false);
        }

        Response.StatusCode = 206;
        Response.Headers.ContentRange = $"bytes {range.Value.Start}-{range.Value.End}/{d.Length}";
        Response.ContentLength = range.Value.Length;
        Response.ContentType = d.ContentType;
        
        await using var source = await d.OpenReadAsync(ct);
        source.Seek(range.Value.Start, SeekOrigin.Begin);
        await CopyRange(source, Response.Body, range.Value.Length, ct);
        
        return new EmptyResult();
    }

    /// <summary>
    /// Returns metadata without opening the blob file.
    /// </summary>
    [HttpHead("{*key}")]
    public async Task<IActionResult> Head(string bucketName, string key, [FromQuery] Guid? versionId, CancellationToken ct)
    {
        var d = await service.GetAsync(OwnerId, bucketName, key, versionId, ct);
        
        ApplyHeaders(d);
        CheckPreconditions(d.ETag);
        
        Response.ContentLength = d.Length;
        Response.ContentType = d.ContentType;
        
        return Ok();
    }

    /// <summary>
    /// Creates a delete marker or removes a specific version.
    /// </summary>
    [HttpDelete("{*key}")]
    public async Task<IActionResult> Delete(string bucketName, string key, [FromQuery] Guid? versionId, CancellationToken ct)
    {
        await service.DeleteAsync(OwnerId, bucketName, key, versionId, ct);
        return NoContent();
    }

    private void ApplyHeaders(DownloadDescriptor d)
    {
        Response.Headers.ETag = d.ETag;
        Response.Headers.AcceptRanges = "bytes";
        Response.Headers["x-version-id"] = d.VersionId.ToString();
        Response.Headers["x-content-sha256"] = d.Sha256;
        
        foreach (var p in d.Metadata)
            Response.Headers["x-meta-" + p.Key] = p.Value;
    }

    private void CheckPreconditions(string etag)
    {
        if (Request.Headers.IfMatch.Count > 0 && !Request.Headers.IfMatch.Any(x => x == etag || x == "*"))
            throw new AppException(412, "if_match_failed", "If-Match does not match.");

        if (Request.Headers.IfNoneMatch.Any(x => x == etag || x == "*"))
            throw new AppException(304, "not_modified", "The object has not been modified.");
    }

    private static ByteRange? ParseRange(string? value, long length)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        if (!value.StartsWith("bytes=") || value.Contains(','))
            throw new AppException(416, "invalid_range", "Only a single byte range is supported.");

        var p = value[6..].Split('-', 2);
        if (!long.TryParse(p[0], out var start) || start < 0 || start >= length)
            throw new AppException(416, "invalid_range", "The requested range is outside the object.");

        var end = string.IsNullOrEmpty(p[1]) ? length - 1
            : long.TryParse(p[1], out var e) ? Math.Min(e, length - 1) : -1;
        
        if (end < start)
            throw new AppException(416, "invalid_range", "The requested range is invalid.");

        return new(start, end);
    }
    private static async Task CopyRange(Stream input, Stream output, long remaining, CancellationToken ct)
    {
        var buffer = new byte[64 * 1024];
        
        while (remaining > 0)
        {
            var read = await input.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, remaining)), ct);
            if (read == 0)
                throw new EndOfStreamException();
            
            await output.WriteAsync(buffer.AsMemory(0, read), ct);
            remaining -= read;
        }
    }
}

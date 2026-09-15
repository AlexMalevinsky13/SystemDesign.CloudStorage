using System;
using System.Collections.Generic;
using System.Text;

namespace SystemDesign.CloudStorage.Application.Contracts;

/// <summary>
/// An object version response.
/// </summary>
public sealed record ObjectVersionDto(
    Guid VersionId,
    bool IsDeleteMarker,
    long ContentLength,
    string? ETag,
    DateTimeOffset CreatedAtUtc);

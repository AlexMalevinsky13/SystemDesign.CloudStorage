namespace SystemDesign.CloudStorage.Application.Contracts;

/// <summary>
/// A requested byte range.
/// </summary>
public readonly record struct ByteRange(long Start, long End)
{
    /// <summary>
    /// The inclusive range length.
    /// </summary>
    public long Length => End - Start + 1;
}

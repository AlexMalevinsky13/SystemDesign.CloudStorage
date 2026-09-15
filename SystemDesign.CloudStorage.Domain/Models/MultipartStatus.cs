namespace SystemDesign.CloudStorage.Domain.Models;

/// <summary>
/// The multipart upload state.
/// </summary>
public enum MultipartStatus
{
    Active,
    Completing,
    Completed,
    Aborted
}

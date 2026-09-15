namespace SystemDesign.CloudStorage.Application.Exceptions;

/// <summary>
/// An application error with HTTP semantics.
/// </summary>
public sealed class AppException(int statusCode, string code, string message) : 
    Exception (message)
{
    /// <summary>
    /// HTTP status.
    /// </summary>
    public int StatusCode { get; } = statusCode;

    /// <summary>
    /// The stable machine-readable error code.
    /// </summary>
    public string Code { get; } = code;
}

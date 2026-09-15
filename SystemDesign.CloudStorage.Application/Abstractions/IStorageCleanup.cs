namespace SystemDesign.CloudStorage.Application.Abstractions;

/// <summary>
/// Cleans temporary and orphaned storage state.
/// </summary>
public interface IStorageCleanup
{
    /// <summary>
    /// Runs one cleanup pass.
    /// </summary>
    Task RunAsync(CancellationToken cancellationToken);
}

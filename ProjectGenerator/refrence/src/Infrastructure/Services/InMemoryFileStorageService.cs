namespace Arsis.Infrastructure.Services;

public sealed class InMemoryFileStorageService
{
    private readonly Dictionary<string, byte[]> _files = new();

    public Task StoreAsync(string fileName, byte[] content, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(content);
        _files[fileName] = content;
        return Task.CompletedTask;
    }

    public Task<byte[]?> RetrieveAsync(string fileName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        _files.TryGetValue(fileName, out var content);
        return Task.FromResult(content);
    }
}

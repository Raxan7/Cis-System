using Cis.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Cis.Infrastructure.Integrations;

internal sealed class LocalDocumentStorage : IDocumentStorage
{
    private readonly string _rootPath;

    public LocalDocumentStorage(IConfiguration configuration)
    {
        _rootPath = configuration["Storage:LocalPath"]
            ?? configuration["Integrations:LocalStoragePath"]
            ?? Path.Combine(AppContext.BaseDirectory, "integration-storage");
    }

    public async Task<string> StoreAsync(string fileName, string content, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_rootPath);
        var safeFileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            safeFileName = "payload.txt";
        }

        var storedName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}-{safeFileName}";
        var path = Path.Combine(_rootPath, storedName);
        await File.WriteAllTextAsync(path, content, cancellationToken);
        return path;
    }
}

namespace Cis.Application.Common.Interfaces;

public interface IFileSecurityValidator
{
    Task ValidateAsync(FileUploadDescriptor descriptor, CancellationToken cancellationToken = default);
}

public interface IMalwareScanner
{
    Task<MalwareScanResult> ScanAsync(FileUploadDescriptor descriptor, CancellationToken cancellationToken = default);
}

public sealed record FileUploadDescriptor(
    string FileName,
    string ContentType,
    long SizeBytes,
    string StorageReference,
    string Purpose);

public sealed record MalwareScanResult(bool IsClean, string? DetectionName = null)
{
    public static MalwareScanResult Clean() => new(true);

    public static MalwareScanResult Infected(string detectionName) => new(false, detectionName);
}

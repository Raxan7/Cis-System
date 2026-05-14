using System.IO;
using Cis.Application.Common.Exceptions;
using Cis.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace Cis.Infrastructure.Security;

internal sealed class FileSecurityValidator : IFileSecurityValidator
{
    private static readonly char[] InvalidPathCharacters = ['\\', ':', '*', '?', '"', '<', '>', '|'];

    private readonly FileUploadSecurityOptions _options;
    private readonly IMalwareScanner _malwareScanner;

    public FileSecurityValidator(
        IOptions<FileUploadSecurityOptions> options,
        IMalwareScanner malwareScanner)
    {
        _options = options.Value;
        _malwareScanner = malwareScanner;
    }

    public async Task ValidateAsync(FileUploadDescriptor descriptor, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(descriptor.FileName))
        {
            throw Validation("fileName", "File name is required.");
        }

        if (descriptor.FileName.IndexOfAny(['\\', '/']) >= 0)
        {
            throw Validation("fileName", "File name must not contain path separators.");
        }

        if (descriptor.SizeBytes <= 0)
        {
            throw Validation("sizeBytes", "File size must be greater than zero.");
        }

        if (descriptor.SizeBytes > _options.MaxSizeBytes)
        {
            throw Validation("sizeBytes", $"File size exceeds the maximum of {_options.MaxSizeBytes} bytes.");
        }

        var extension = Path.GetExtension(descriptor.FileName)?.Trim().ToLowerInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(extension))
        {
            throw Validation("fileName", "File extension is required.");
        }

        if (_options.BlockedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw Validation("fileName", $"Files with extension '{extension}' are not permitted.");
        }

        if (!_options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw Validation("fileName", $"Files with extension '{extension}' are not allowed.");
        }

        if (string.IsNullOrWhiteSpace(descriptor.ContentType)
            || !_options.AllowedContentTypes.Contains(descriptor.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            throw Validation("contentType", "File content type is not allowed.");
        }

        ValidateStorageReference(descriptor.StorageReference);

        var scanResult = await _malwareScanner.ScanAsync(descriptor, cancellationToken);
        if (!scanResult.IsClean)
        {
            throw Validation("file", $"File failed malware screening{(string.IsNullOrWhiteSpace(scanResult.DetectionName) ? "." : $": {scanResult.DetectionName}.")}");
        }
    }

    private static void ValidateStorageReference(string storageReference)
    {
        if (string.IsNullOrWhiteSpace(storageReference))
        {
            throw Validation("storageReference", "Storage reference is required.");
        }

        if (storageReference.Contains("..", StringComparison.Ordinal)
            || Path.IsPathRooted(storageReference)
            || storageReference.IndexOfAny(InvalidPathCharacters) >= 0)
        {
            throw Validation("storageReference", "Storage reference contains invalid path content.");
        }
    }

    private static ValidationException Validation(string field, string message)
    {
        return new ValidationException(new Dictionary<string, string[]>
        {
            [field] = [message]
        });
    }
}

internal sealed class StubMalwareScanner : IMalwareScanner
{
    public Task<MalwareScanResult> ScanAsync(FileUploadDescriptor descriptor, CancellationToken cancellationToken = default)
    {
        var probe = $"{descriptor.FileName}|{descriptor.StorageReference}|{descriptor.Purpose}";
        if (probe.Contains("malware", StringComparison.OrdinalIgnoreCase)
            || probe.Contains("eicar", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(MalwareScanResult.Infected("StubMalwareSignature"));
        }

        return Task.FromResult(MalwareScanResult.Clean());
    }
}

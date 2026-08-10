using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace SimplexLawFirm.Services.Storage;

public record StoredCaseDocument(string OriginalFileName, string RelativePath, string ContentType, long SizeBytes, string Sha256Hash);

public interface IDocumentStorage
{
    Task<StoredCaseDocument> StoreAsync(int caseId, IFormFile file, CancellationToken ct = default);
    Task<Stream> OpenReadAsync(string relativePath, CancellationToken ct = default);
    Task DeleteAsync(string relativePath, CancellationToken ct = default);
    Task<string> ImportLegacyAsync(int caseId, string legacyFullPath, CancellationToken ct = default);
}

public sealed class SecureDocumentStorage(IWebHostEnvironment environment) : IDocumentStorage
{
    public const long MaximumBytes = 50L * 1024 * 1024;
    private readonly string root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "App_Data", "SecureCaseDocuments"));

    public async Task<StoredCaseDocument> StoreAsync(int caseId, IFormFile file, CancellationToken ct = default)
    {
        if (caseId < 0) throw new ArgumentOutOfRangeException(nameof(caseId));
        if (file.Length is <= 0 or > MaximumBytes) throw new InvalidDataException("The file must be between 1 byte and 50 MB.");
        var original = Regex.Replace(Path.GetFileName(file.FileName), @"[^\w.\- ]", "_");
        var relative = $"{caseId}/{Guid.NewGuid():N}.bin";
        var full = Resolve(relative); Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await using var input = file.OpenReadStream(); await using var output = new FileStream(full, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        using var sha = SHA256.Create(); var buffer = new byte[81920]; long size = 0; int count;
        while ((count = await input.ReadAsync(buffer, ct)) > 0)
        {
            size += count; if (size > MaximumBytes) throw new InvalidDataException("The file exceeds 50 MB.");
            sha.TransformBlock(buffer, 0, count, null, 0); await output.WriteAsync(buffer.AsMemory(0, count), ct);
        }
        sha.TransformFinalBlock([], 0, 0);
        return new(original[..Math.Min(original.Length, 200)], relative, string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType, size, Convert.ToHexString(sha.Hash!));
    }

    public Task<Stream> OpenReadAsync(string relativePath, CancellationToken ct = default) =>
        Task.FromResult<Stream>(new FileStream(Resolve(relativePath), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true));

    public Task DeleteAsync(string relativePath, CancellationToken ct = default)
    {
        var full = Resolve(relativePath); if (File.Exists(full)) File.Delete(full); return Task.CompletedTask;
    }

    public async Task<string> ImportLegacyAsync(int caseId, string legacyFullPath, CancellationToken ct = default)
    {
        var relative = $"{Math.Max(caseId, 0)}/{Guid.NewGuid():N}.bin"; var destination = Resolve(relative);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        await using (var input = new FileStream(legacyFullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true))
        await using (var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true)) await input.CopyToAsync(output, ct);
        File.Delete(legacyFullPath); return relative;
    }

    private string Resolve(string relative)
    {
        if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative)) throw new InvalidDataException("Invalid document storage path.");
        var full = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
        if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Invalid document storage path.");
        return full;
    }
}

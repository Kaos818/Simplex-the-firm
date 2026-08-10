using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace SimplexLawFirm.Services.Storage;

public interface IReimbursementProofStorage
{
    Task<SecureStoredFile> StoreAsync(int claimId, IFormFile file, CancellationToken ct = default);
    Task<Stream> OpenReadAsync(string relativePath, CancellationToken ct = default);
    Task DeleteAsync(string relativePath, CancellationToken ct = default);
}

public sealed class ReimbursementProofStorage(IWebHostEnvironment environment) : IReimbursementProofStorage
{
    public const long MaximumBytes = 10 * 1024 * 1024;
    private readonly string root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "App_Data", "SecureReimbursementProofs"));

    public async Task<SecureStoredFile> StoreAsync(int claimId, IFormFile file, CancellationToken ct = default)
    {
        if (claimId <= 0) throw new ArgumentOutOfRangeException(nameof(claimId));
        if (file.Length is <= 0 or > MaximumBytes) throw new InvalidDataException("Proof must be between 1 byte and 10 MB.");
        await using var input = file.OpenReadStream();
        var header = new byte[8];
        var read = await input.ReadAsync(header, ct);
        input.Position = 0;
        var contentType = Detect(header.AsSpan(0, read)) ?? throw new InvalidDataException("Only valid PDF, JPEG, and PNG proof is accepted.");
        var relative = Path.Combine(claimId.ToString(), $"{Guid.NewGuid():N}.bin");
        var full = Resolve(relative);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await using var output = new FileStream(full, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        using var sha = SHA256.Create();
        var buffer = new byte[81920];
        int count;
        while ((count = await input.ReadAsync(buffer, ct)) > 0)
        {
            sha.TransformBlock(buffer, 0, count, null, 0);
            await output.WriteAsync(buffer.AsMemory(0, count), ct);
        }
        sha.TransformFinalBlock([], 0, 0);
        var name = Regex.Replace(Path.GetFileName(file.FileName), @"[^\w.\- ]", "_");
        return new(name[..Math.Min(name.Length, 200)], Path.GetFileName(full), relative.Replace('\\', '/'), contentType, file.Length, Convert.ToHexString(sha.Hash!));
    }

    public Task<Stream> OpenReadAsync(string relativePath, CancellationToken ct = default) =>
        Task.FromResult<Stream>(new FileStream(Resolve(relativePath), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true));

    public Task DeleteAsync(string relativePath, CancellationToken ct = default)
    {
        var full = Resolve(relativePath);
        if (File.Exists(full)) File.Delete(full);
        return Task.CompletedTask;
    }

    private string Resolve(string relative)
    {
        if (Path.IsPathRooted(relative)) throw new InvalidDataException("Invalid proof path.");
        var full = Path.GetFullPath(Path.Combine(root, relative));
        if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Invalid proof path.");
        return full;
    }

    private static string? Detect(ReadOnlySpan<byte> header) =>
        header.StartsWith("%PDF-"u8) ? "application/pdf" :
        header.Length >= 3 && header[0] == 0xff && header[1] == 0xd8 && header[2] == 0xff ? "image/jpeg" :
        header.StartsWith(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }) ? "image/png" : null;
}

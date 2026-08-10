using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace SimplexLawFirm.Services.Storage;

public sealed class LocalSecureFileStorage(IWebHostEnvironment environment) : ISecureFileStorage
{
    public const long MaximumBytes = 10 * 1024 * 1024;
    private readonly string _root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "App_Data", "SecureBeneficiaryDocuments"));

    public async Task<SecureStoredFile> StoreAsync(int beneficiaryId, IFormFile file, CancellationToken cancellationToken = default)
    {
        if (beneficiaryId <= 0) throw new ArgumentOutOfRangeException(nameof(beneficiaryId));
        if (file.Length is <= 0 or > MaximumBytes) throw new InvalidDataException("The file must be between 1 byte and 10 MB.");
        await using var input = file.OpenReadStream();
        var header = new byte[8];
        var read = await input.ReadAsync(header, cancellationToken);
        input.Position = 0;
        var contentType = Detect(header.AsSpan(0, read));
        if (contentType is null) throw new InvalidDataException("Only valid PDF, JPEG, and PNG files are accepted.");
        if (contentType == "application/pdf")
        {
            using var reader = new StreamReader(input, leaveOpen: true);
            var text = await reader.ReadToEndAsync(cancellationToken);
            input.Position = 0;
            if (Regex.Matches(text, @"/Type\s*/Page\b").Count > 20) throw new InvalidDataException("PDF files may contain at most 20 pages.");
        }
        var relative = Path.Combine(beneficiaryId.ToString(), $"{Guid.NewGuid():N}.bin");
        var full = Resolve(relative);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await using var output = new FileStream(full, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        using var sha = SHA256.Create();
        var buffer = new byte[81920];
        int count;
        long size = 0;
        while ((count = await input.ReadAsync(buffer, cancellationToken)) > 0)
        {
            size += count;
            if (size > MaximumBytes) throw new InvalidDataException("The file exceeds 10 MB.");
            sha.TransformBlock(buffer, 0, count, null, 0);
            await output.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
        }
        sha.TransformFinalBlock([], 0, 0);
        var display = Path.GetFileName(file.FileName);
        display = Regex.Replace(display, @"[^\w.\- ]", "_");
        return new(display[..Math.Min(display.Length, 200)], Path.GetFileName(full), relative.Replace('\\', '/'), contentType, size, Convert.ToHexString(sha.Hash!));
    }

    public Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default) =>
        Task.FromResult<Stream>(new FileStream(Resolve(relativePath), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true));

    private string Resolve(string relative)
    {
        if (Path.IsPathRooted(relative)) throw new InvalidDataException("Invalid storage path.");
        var full = Path.GetFullPath(Path.Combine(_root, relative));
        if (!full.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Invalid storage path.");
        return full;
    }

    private static string? Detect(ReadOnlySpan<byte> h) =>
        h.StartsWith("%PDF-"u8) ? "application/pdf" :
        h.Length >= 3 && h[0] == 0xff && h[1] == 0xd8 && h[2] == 0xff ? "image/jpeg" :
        h.StartsWith(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }) ? "image/png" : null;
}

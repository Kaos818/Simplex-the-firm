namespace SimplexLawFirm.Services.Storage;

public record SecureStoredFile(string OriginalFileName, string StoredFileName, string RelativePath, string ContentType, long SizeBytes, string Sha256Hash);

public interface ISecureFileStorage
{
    Task<SecureStoredFile> StoreAsync(int beneficiaryId, IFormFile file, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default);
}

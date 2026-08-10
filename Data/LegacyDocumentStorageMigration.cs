using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Services.Storage;

namespace SimplexLawFirm.Data;

public static class LegacyDocumentStorageMigration
{
    public static async Task MigrateAsync(ApplicationDbContext db, IWebHostEnvironment environment, IDocumentStorage storage, CancellationToken ct = default)
    {
        var legacy = await db.Documents.Where(x => x.FilePath.StartsWith("/uploads/") || x.FilePath.StartsWith("uploads/")).ToListAsync(ct);
        foreach (var document in legacy)
        {
            var relative = document.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var source = Path.GetFullPath(Path.Combine(environment.WebRootPath, relative));
            if (!source.StartsWith(Path.GetFullPath(environment.WebRootPath) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !File.Exists(source)) continue;
            document.FilePath = await storage.ImportLegacyAsync(document.CaseId ?? 0, source, ct);
        }
        if (legacy.Count > 0) await db.SaveChangesAsync(ct);
        var publicDocumentFolder = Path.Combine(environment.WebRootPath, "uploads", "documents");
        if (Directory.Exists(publicDocumentFolder) && !Directory.EnumerateFileSystemEntries(publicDocumentFolder).Any()) Directory.Delete(publicDocumentFolder);
    }
}

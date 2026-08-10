using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models.Beneficiaries;
using SimplexLawFirm.Services.Storage;

namespace SimplexLawFirm.Services.Verification;

/// <summary>
/// Selects the only authorised one-to-one reference source. Cropping and ambiguity
/// rejection occur inside the loopback ML service; the document is never sent to a browser.
/// </summary>
public sealed class ReferenceFaceExtractor(ApplicationDbContext db, ISecureFileStorage storage) : IReferenceFaceExtractor
{
    public async Task<ReferenceFaceSource?> OpenLatestAsync(int beneficiaryId, CancellationToken cancellationToken = default)
    {
        if (beneficiaryId <= 0) return null;
        var document = await db.BeneficiaryDocuments.AsNoTracking().Include(x => x.Requirement)
            .Where(x => x.BeneficiaryId == beneficiaryId && x.Requirement.Code == "SA_ID" &&
                x.PreScreenStatus == DocumentPreScreenStatus.Passed)
            .OrderByDescending(x => x.UploadedAtUtc).FirstOrDefaultAsync(cancellationToken);
        if (document is null) return null;
        return new ReferenceFaceSource(await storage.OpenReadAsync(document.RelativeStoragePath, cancellationToken), document.ContentType);
    }
}

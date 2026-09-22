using SimplexLawFirm.Data;
using SimplexLawFirm.Models;

namespace SimplexLawFirm.Services
{
    /// <summary>
    /// Default implementation that writes to the AuditEntries table.
    /// If you already have an AuditService, skip this file.
    /// </summary>
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _db;

        public AuditService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task LogAsync(string entityType, string entityId, string action,
            string? safeMetadata = null, int? actorUserId = null)
        {
            _db.AuditEntries.Add(new AuditEntry
            {
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                SafeMetadataJson = safeMetadata,
                ActorUserId = actorUserId,
                CreatedAtUtc = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
    }
}

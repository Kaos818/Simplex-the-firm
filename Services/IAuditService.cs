namespace SimplexLawFirm.Services
{
    /// <summary>
    /// Centralised audit-logging service.
    /// If you already have this interface, skip this file.
    /// </summary>
    public interface IAuditService
    {
        /// <summary>
        /// Append an immutable audit entry.
        /// </summary>
        Task LogAsync(string entityType, string entityId, string action,
            string? safeMetadata = null, int? actorUserId = null);
    }
}

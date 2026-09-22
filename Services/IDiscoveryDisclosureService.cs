using SimplexLawFirm.DTOs.Discovery;
using SimplexLawFirm.Models;

namespace SimplexLawFirm.Services
{
    public interface IDiscoveryDisclosureService
    {
        // Compatibility API retained for the original end-to-end workflow tests.
        Task<DiscoveryBundle> CreateAsync(int caseId, int attorneyId, string opposingName, string opposingEmail, IEnumerable<int> documentIds, IDictionary<int, string> privilegeReasons);
        Task DecideAsync(int itemId, int directorId, bool withhold, string reason);
        Task FinaliseAsync(int bundleId, int attorneyId, string exchangeUrl);
        Task MarkAccessedAsync(string token);
        Task RecordOpposingBundleAsync(int bundleId, IEnumerable<(string FileName, string RelativePath, string ContentType, long SizeBytes)> files);
        Task SetComplianceAsync(int bundleId, int attorneyId, bool compliant, string notes);
        Task EscalateGapsAsync(int bundleId, int attorneyId);
        // Attorney flow
        Task<DiscoveryBundleDetailDto> CreateBundleAsync(int attorneyId, CreateDiscoveryBundleDto dto);
        Task<DiscoveryBundleDetailDto> FinaliseAndExchangeAsync(int attorneyId, int bundleId, int linkValidityHours = 72);
        Task<DiscoveryBundleDetailDto> RecordComplianceCheckAsync(int attorneyId, ComplianceCheckDto dto);
        Task<DiscoveryBundleDetailDto> EscalateMissingDisclosureAsync(int attorneyId, int bundleId);
        Task<List<DiscoveryBundle>> GetIndexForRoleAsync(string role, int userId);
        Task<DiscoveryBundle?> GetDetailsAsync(int id);
        Task<Case?> GetCaseForCreateAsync(int caseId, int attorneyId);

        // Director flow
        Task<DiscoveryBundleDetailDto> DecidePrivilegeAsync(int directorId, PrivilegeDecisionDto dto);

        // External portal flow (token-authenticated, no user account)
        Task<DiscoveryBundle?> GetPortalBundleAsync(string token);
        Task<bool> TryRecordPortalAccessAsync(string token);
        Task<(bool Success, string? Error)> SubmitOpposingBundleAsync(string token, IEnumerable<(string FileName, string ContentType, long SizeBytes, Stream Content)> files);
        Task<(Stream? Stream, string? FileName, string? ContentType)> GetDisclosedDocumentStreamAsync(string token, int documentId);
        Task<(Stream? Stream, string? FileName)> GetOpposingDocumentStreamAsync(int userId, int opposingDocumentId, string role);
    }
}

namespace SimplexLawFirm.Services.Verification;
public interface ILocalVerificationClient
{
    Task<string> AnalyseDocumentAsync(Stream file, string fileName, string requirementCode, bool certified, bool expiryCheck, CancellationToken cancellationToken);
    Task<string> VerifyFaceAsync(Stream referenceImage, IReadOnlyList<byte[]> frames, Guid sessionId,
        IReadOnlyList<string> serverChallenges, IReadOnlyList<long> timestamps,
        IReadOnlyList<int> stageIndexes, CancellationToken cancellationToken);
}

namespace SimplexLawFirm.Services.Verification;

public interface IReferenceFaceExtractor
{
    Task<ReferenceFaceSource?> OpenLatestAsync(int beneficiaryId, CancellationToken cancellationToken = default);
}

public sealed class ReferenceFaceSource(Stream content, string contentType) : IAsyncDisposable
{
    public Stream Content { get; } = content;
    public string ContentType { get; } = contentType;
    public ValueTask DisposeAsync() => Content.DisposeAsync();
}

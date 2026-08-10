using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SimplexLawFirm.Services.Verification;

public sealed class LocalVerificationClient(HttpClient http, IOptions<VerificationOptions> options) : ILocalVerificationClient
{
    public async Task<string> AnalyseDocumentAsync(Stream file, string fileName, string requirementCode, bool certified, bool expiryCheck, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ApiKey)) throw new InvalidOperationException("Local verification API key is not configured.");
        http.BaseAddress = new Uri(settings.BaseUrl);
        http.Timeout = TimeSpan.FromSeconds(Math.Clamp(settings.TimeoutSeconds, 5, 120));
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/documents/analyse");
        request.Headers.Add("X-Api-Key", settings.ApiKey);
        using var form = new MultipartFormDataContent
        {
            { new StreamContent(file), "file", Path.GetFileName(fileName) },
            { new StringContent(requirementCode), "requirement_code" },
            { new StringContent(certified.ToString().ToLowerInvariant()), "requires_certified_copy" },
            { new StringContent(expiryCheck.ToString().ToLowerInvariant()), "requires_expiry_check" }
        };
        request.Content = form;
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await ReadBoundedAsync(response.Content, cancellationToken);
    }

    public async Task<string> VerifyFaceAsync(Stream referenceImage, IReadOnlyList<byte[]> frames, Guid sessionId,
        IReadOnlyList<string> serverChallenges, IReadOnlyList<long> timestamps, IReadOnlyList<int> stageIndexes,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ApiKey)) throw new InvalidOperationException("Local verification API key is not configured.");
        if (frames.Count is < 20 or > 60 || timestamps.Count != frames.Count || stageIndexes.Count != frames.Count)
            throw new InvalidDataException("Capture must contain 20 to 60 correctly indexed frames.");
        http.BaseAddress = new Uri(settings.BaseUrl);
        http.Timeout = TimeSpan.FromSeconds(Math.Clamp(settings.TimeoutSeconds, 5, 120));
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/faces/verify");
        request.Headers.Add("X-Api-Key", settings.ApiKey);
        using var form = new MultipartFormDataContent();
        var reference = new StreamContent(referenceImage);
        reference.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(reference, "reference", "reference.bin");
        form.Add(new StringContent(JsonSerializer.Serialize(new {
            session_id = sessionId, challenges = serverChallenges, timestamps, stage_indexes = stageIndexes
        })), "payload");
        for (var index = 0; index < frames.Count; index++)
        {
            var content = new ByteArrayContent(frames[index]);
            content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            form.Add(content, "frames", $"frame-{index:D2}.jpg");
        }
        request.Content = form;
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await ReadBoundedAsync(response.Content, cancellationToken);
    }

    private static async Task<string> ReadBoundedAsync(HttpContent content, CancellationToken cancellationToken)
    {
        const int maximum = 256_000;
        if (content.Headers.ContentLength > maximum) throw new InvalidDataException("Verification response was too large.");
        await using var source = await content.ReadAsStreamAsync(cancellationToken);
        using var target = new MemoryStream();
        var buffer = new byte[16_384];
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (target.Length + read > maximum) throw new InvalidDataException("Verification response was too large.");
            await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
        return System.Text.Encoding.UTF8.GetString(target.ToArray());
    }
}

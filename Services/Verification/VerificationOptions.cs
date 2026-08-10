namespace SimplexLawFirm.Services.Verification;
public sealed class VerificationOptions
{
    public string BaseUrl { get; set; } = "http://127.0.0.1:8091";
    public int TimeoutSeconds { get; set; } = 60;
    public string ApiKey { get; set; } = "";
}

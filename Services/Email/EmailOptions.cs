namespace SimplexLawFirm.Services.Email;
public sealed class EmailOptions
{
    // Prefer this in Azure. It authenticates directly with the Communication Services resource
    // and avoids keeping an SMTP username/password in the app.
    public string AzureCommunicationConnectionString { get; set; } = "";
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public bool UseStartTls { get; set; } = true;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromAddress { get; set; } = "";
    public string FromName { get; set; } = "Simplex Law Firm";
    public string ReplyToAddress { get; set; } = "";
    public string PublicBaseUrl { get; set; } = "";
}

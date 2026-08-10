using System.Text;
using SimplexLawFirm.Models;
using SimplexLawFirm.Models.Beneficiaries;

namespace SimplexLawFirm.Services;

public interface IRecordPdfService
{
    byte[] CreateReimbursement(ReimbursementClaim claim);
    byte[] CreateFacialVerification(Beneficiary beneficiary, FacialVerificationSession? session, IEnumerable<AuditEntry> audit);
    byte[] CreateInvoice(Invoice invoice);
}

public sealed class RecordPdfService : IRecordPdfService
{
    public byte[] CreateReimbursement(ReimbursementClaim claim) => Build(
        "REIMBURSEMENT CLAIM",
        [
            ("Claim", claim.ClaimNumber), ("Status", claim.Status.ToString()),
            ("Attorney", claim.Attorney.FullName), ("Matter", $"{claim.Case.CaseNumber} - {claim.Case.Title}"),
            ("Expense", claim.ExpenseType.ToString()), ("Expense date", claim.ExpenseDate.ToString("dd MMMM yyyy")),
            ("Amount", Money(claim.Amount)), ("Description", claim.Description),
            ("Matched activity", claim.MatchedActivityType is null ? "None" : $"{claim.MatchedActivityType} #{claim.MatchedActivityId}"),
            ("Classification", claim.Classification.ToString()), ("Classification basis", claim.ClassificationReason),
            ("Policy limit", Money(claim.PolicyLimitSnapshot)), ("Delegated limit", Money(claim.DelegatedLimitSnapshot)),
            ("Proof file", claim.ProofOriginalFileName ?? "Not uploaded"), ("Proof SHA-256", claim.ProofSha256Hash ?? "Not available"),
            ("Decision", claim.DecisionReason ?? claim.ValidationFailureReason ?? "No decision recorded"),
            ("Submitted", $"{claim.SubmittedAtUtc:u}"), ("Decided", claim.DecidedAtUtc?.ToString("u") ?? "Not decided"),
            ("Audit trail", string.Join(" | ", claim.AuditEntries.OrderBy(x => x.RecordedAtUtc).Select(x => $"{x.RecordedAtUtc:u} {x.Action}: {x.Details}")))
        ]);

    public byte[] CreateFacialVerification(Beneficiary beneficiary, FacialVerificationSession? session, IEnumerable<AuditEntry> audit) => Build(
        "FACIAL VERIFICATION REPORT",
        [
            ("Beneficiary", $"{beneficiary.FirstName} {beneficiary.LastName}"),
            ("Application status", beneficiary.Status.ToString()), ("Verification session", session?.Id.ToString() ?? "No session"),
            ("Outcome", session?.Status.ToString() ?? "No result"),
            ("Consent", session?.ConsentGranted == true ? $"Granted {session.ConsentGrantedAtUtc:u} - notice {session.ConsentNoticeVersion}" : "Not recorded"),
            ("Liveness", Result(session?.LivenessPassed)), ("Face match", Result(session?.FaceMatched)),
            ("Similarity score", session?.SimilarityScore?.ToString("P1") ?? "Not available"),
            ("Valid frame ratio", session?.ValidFrameRatio?.ToString("P1") ?? "Not available"),
            ("Duplicate frame ratio", session?.DuplicateFrameRatio?.ToString("P1") ?? "Not available"),
            ("Reason code", session?.ResultReasonCode ?? "None"), ("Reason", session?.ResultReason ?? "None"),
            ("Created", session?.CreatedAtUtc.ToString("u") ?? "Not available"),
            ("Completed", session?.CompletedAtUtc?.ToString("u") ?? "Not completed"),
            ("Audit trail", string.Join(" | ", audit.OrderBy(x => x.CreatedAtUtc).Select(x => $"{x.CreatedAtUtc:u} {x.Action}"))),
            ("Privacy note", "This report records verification decisions and metrics only. It does not embed biometric capture images.")
        ]);

    public byte[] CreateInvoice(Invoice invoice) => Build(
        "INVOICE",
        [
            ("Invoice", invoice.InvoiceNumber), ("Status", invoice.Status.ToString()),
            ("Client", invoice.Client.FullName), ("Matter", invoice.Case is null ? "Not linked" : $"{invoice.Case.CaseNumber} - {invoice.Case.Title}"),
            ("Issue date", invoice.IssueDate.ToString("dd MMMM yyyy")), ("Due date", invoice.DueDate.ToString("dd MMMM yyyy")),
            ("Description", invoice.Description ?? invoice.Notes), ("Subtotal", Money(invoice.Amount)),
            ("Tax", Money(invoice.TaxAmount)), ("Total", Money(invoice.TotalAmount)),
            ("Payments", Money(invoice.Payments.Sum(x => x.Amount))),
            ("Balance due", Money(invoice.TotalAmount - invoice.Payments.Sum(x => x.Amount))),
            ("Disbursements", invoice.Disbursements.Count == 0 ? "None" : string.Join(" | ", invoice.Disbursements.Select(x => $"{x.Description}: {Money(x.Amount)}")))
        ]);

    private static string Result(bool? value) => value == true ? "Passed" : value == false ? "Not passed" : "Requires review";
    private static string Money(decimal value) => $"R {value:N2}";

    private static byte[] Build(string title, IEnumerable<(string Label, string Value)> fields)
    {
        var lines = new List<(string Text, int Size, bool Bold)> { ("SIMPLEX LAW FIRM", 11, true), (title, 20, true), ($"Generated {DateTime.UtcNow:u}", 9, false), ("", 8, false) };
        foreach (var (label, value) in fields)
        {
            lines.Add((label.ToUpperInvariant(), 9, true));
            foreach (var line in Wrap(value, 88)) lines.Add((line, 10, false));
            lines.Add(("", 5, false));
        }
        return Pdf(lines);
    }

    private static IEnumerable<string> Wrap(string? value, int width)
    {
        var remaining = Clean(value ?? "");
        if (remaining.Length == 0) { yield return "Not recorded"; yield break; }
        while (remaining.Length > width)
        {
            var split = remaining.LastIndexOf(' ', width);
            if (split < 1) split = width;
            yield return remaining[..split].Trim();
            remaining = remaining[split..].TrimStart();
        }
        yield return remaining;
    }

    private static byte[] Pdf(List<(string Text, int Size, bool Bold)> lines)
    {
        var pages = new List<List<(string Text, int Size, bool Bold)>>();
        var page = new List<(string, int, bool)>(); var y = 790;
        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            var advance = Math.Max(12, line.Size + 5);
            var nextAdvance = index + 1 < lines.Count ? Math.Max(12, lines[index + 1].Size + 5) : 0;
            if (y - advance < 45 || line.Bold && y - advance - nextAdvance < 45) { pages.Add(page); page = []; y = 790; }
            page.Add(line); y -= advance;
        }
        if (page.Count > 0) pages.Add(page);

        var objects = new List<string>();
        var pageObjectIds = Enumerable.Range(0, pages.Count).Select(i => 3 + i * 2).ToArray();
        objects.Add("<< /Type /Catalog /Pages 2 0 R >>");
        objects.Add($"<< /Type /Pages /Kids [{string.Join(' ', pageObjectIds.Select(x => $"{x} 0 R"))}] /Count {pages.Count} >>");
        for (var i = 0; i < pages.Count; i++)
        {
            var contentId = pageObjectIds[i] + 1;
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 {3 + pages.Count * 2} 0 R /F2 {4 + pages.Count * 2} 0 R >> >> /Contents {contentId} 0 R >>");
            var stream = new StringBuilder("BT\n"); y = 790;
            foreach (var line in pages[i]) { stream.Append($"/{(line.Bold ? "F2" : "F1")} {line.Size} Tf 1 0 0 1 55 {y} Tm ({Escape(line.Text)}) Tj\n"); y -= Math.Max(12, line.Size + 5); }
            stream.Append($"/F1 8 Tf 1 0 0 1 520 25 Tm (Page {i + 1} of {pages.Count}) Tj\nET");
            var value = stream.ToString(); objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(value)} >>\nstream\n{value}\nendstream");
        }
        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>");
        using var output = new MemoryStream(); void Write(string value) => output.Write(Encoding.ASCII.GetBytes(value));
        Write("%PDF-1.4\n"); var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Count; i++) { offsets.Add(output.Position); Write($"{i + 1} 0 obj\n{objects[i]}\nendobj\n"); }
        var xref = output.Position; Write($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) Write($"{offset:0000000000} 00000 n \n");
        Write($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF"); return output.ToArray();
    }

    private static string Clean(string value) => string.Concat(value.Normalize(NormalizationForm.FormD).Where(c => c <= 127 && !char.IsControl(c) || c == ' ')).Replace("  ", " ").Trim();
    private static string Escape(string value) => Clean(value).Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
}

using SimplexLawFirm.Models;
using SimplexLawFirm.Models.Beneficiaries;
using SimplexLawFirm.Services;
using Xunit;

namespace SimplexLawFirm.Tests;

public class RecordPdfTests
{
    private readonly RecordPdfService pdf = new();

    [Fact]
    public void ReimbursementPdf_IsDownloadablePdf()
    {
        var bytes = pdf.CreateReimbursement(new ReimbursementClaim
        {
            ClaimNumber = "REIM-000042", Attorney = new ApplicationUser { FullName = "Test Attorney" },
            Case = new Case { CaseNumber = "MAT-42", Title = "Test matter" }, Amount = 1250,
            Description = "Court filing fee", ClassificationReason = "Recoverable under matter terms"
        });
        AssertPdf(bytes, "REIMBURSEMENT CLAIM");
        WriteSample("reimbursement-sample.pdf", bytes);
    }

    [Fact]
    public void FacialVerificationPdf_IsDownloadableWithoutEmbeddingBiometricImages()
    {
        var bytes = pdf.CreateFacialVerification(
            new Beneficiary { FirstName = "Test", LastName = "Beneficiary" },
            new FacialVerificationSession { Id = Guid.NewGuid(), Status = FacialVerificationStatus.Verified, LivenessPassed = true, FaceMatched = true }, []);
        AssertPdf(bytes, "FACIAL VERIFICATION REPORT");
        Assert.DoesNotContain("/Subtype /Image", System.Text.Encoding.ASCII.GetString(bytes));
        WriteSample("face-verification-sample.pdf", bytes);
    }

    [Fact]
    public void InvoicePdf_IsDownloadableWhenNoStoredPdfPathExists()
    {
        var bytes = pdf.CreateInvoice(new Invoice
        {
            InvoiceNumber = "INV-000042", Client = new Client { FirstName = "Test", LastName = "Client" },
            Amount = 1000, TaxAmount = 150, TotalAmount = 1150, Description = "Legal services"
        });
        AssertPdf(bytes, "INVOICE");
        WriteSample("invoice-sample.pdf", bytes);
    }

    private static void AssertPdf(byte[] bytes, string expectedText)
    {
        var text = System.Text.Encoding.ASCII.GetString(bytes);
        Assert.StartsWith("%PDF-", text);
        Assert.Contains(expectedText, text);
        Assert.EndsWith("%%EOF", text);
        Assert.True(bytes.Length > 500);
    }

    private static void WriteSample(string name, byte[] bytes)
    {
        var directory = Environment.GetEnvironmentVariable("SIMPLEX_PDF_SAMPLE_DIR");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory, name), bytes);
    }
}

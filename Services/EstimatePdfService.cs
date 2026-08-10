using System.Text;
using SimplexLawFirm.Models;

namespace SimplexLawFirm.Services;

public interface IEstimatePdfService { byte[] Create(MatterCostEstimate estimate); }

public sealed class EstimatePdfService : IEstimatePdfService
{
    public byte[] Create(MatterCostEstimate estimate)
    {
        var e = estimate.Enquiry;
        var lines = new List<(string Text, int Size, bool Bold)> {
            ("SIMPLEX LAW FIRM", 11, true), ("MATTER COST ESTIMATE", 22, true),
            ($"Estimate #{estimate.Id:000000} / Version {estimate.Version}", 10, false),
            ($"Issued {estimate.LockedAtUtc:dd MMMM yyyy} UTC", 10, false), ("", 8, false),
            ($"Prepared for: {e.ContactName}", 11, true), ($"Matter type: {e.MatterType}", 10, false),
            ($"Estimated range: {Money(estimate.TotalLow)} - {Money(estimate.TotalHigh)}", 16, true), ("", 8, false),
            ("COST BREAKDOWN", 12, true),
            ($"Professional fees: {Money(estimate.ProfessionalFeesLow)} - {Money(estimate.ProfessionalFeesHigh)}", 10, false),
            ($"Expected disbursements: {Money(estimate.DisbursementsLow)} - {Money(estimate.DisbursementsHigh)}", 10, false),
            ($"VAT (15%): {Money(estimate.VatLow)} - {Money(estimate.VatHigh)}", 10, false), ("", 8, false),
            ("ASSUMPTIONS", 12, true),
            ($"Matter value: {Money(e.MatterValue)}", 10, false), ($"Urgency: {e.Urgency}", 10, false),
            ($"Court proceedings: {(e.RequiresCourtProceedings ? "Required" : "Not currently required")}", 10, false),
            ($"Documents available: {e.DocumentReadiness}", 10, false),
            ($"Historical basis: {estimate.ComparableMatterCount} comparable closed matters", 10, false), ("", 8, false),
            ("IMPORTANT", 12, true),
            ("This estimate is indicative and is not a binding quotation. It is based on the", 9, false),
            ("facts, assumptions and charge-out rates recorded when it was issued. Material", 9, false),
            ("changes in scope may affect the final cost. VAT and disbursements are estimates.", 9, false),
            ("The firm retains this locked estimate for billing variance governance.", 9, false)
        };
        return BuildPdf(lines);
    }

    private static string Money(decimal value) => $"R {value:N2}";
    private static byte[] BuildPdf(List<(string Text, int Size, bool Bold)> lines)
    {
        var content = new StringBuilder("BT\n");
        var y = 790;
        foreach (var line in lines)
        {
            content.Append($"/{(line.Bold ? "F2" : "F1")} {line.Size} Tf 1 0 0 1 55 {y} Tm ({Escape(line.Text)}) Tj\n");
            y -= Math.Max(14, line.Size + 6);
        }
        content.Append("ET");
        var stream = content.ToString();
        var objects = new[] {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 5 0 R /F2 6 0 R >> >> /Contents 4 0 R >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>"
        };
        using var output = new MemoryStream();
        void Write(string value) { var bytes = Encoding.ASCII.GetBytes(value); output.Write(bytes); }
        Write("%PDF-1.4\n");
        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Length; i++) { offsets.Add(output.Position); Write($"{i + 1} 0 obj\n{objects[i]}\nendobj\n"); }
        var xref = output.Position;
        Write($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) Write($"{offset:0000000000} 00000 n \n");
        Write($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        return output.ToArray();
    }
    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("–", "-").Replace("—", "-");
}

using System.Data;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services.Email;
using SimplexLawFirm.Services.Notifications;

namespace SimplexLawFirm.Services.Billing;
public sealed class AppointmentBillingService(ApplicationDbContext db, IEmailService email, INotificationService? notifications = null, IConfiguration? configuration = null) : IAppointmentBillingService
{
    public async Task<int> ProcessDueAsync(DateTime nowUtc, CancellationToken ct = default)
    {
        var ids = await db.CalendarEvents.AsNoTracking().Where(x => x.ClientResponseStatus == AppointmentResponseStatus.Accepted && (x.LawyerApprovalStatus == AppointmentApprovalStatus.Approved || x.LawyerApprovalStatus == AppointmentApprovalStatus.NotRequired) && x.EndDateTime <= nowUtc && x.Status != EventStatus.Cancelled && !x.BillingProcessed && x.AppointmentFee > 0).Select(x => x.Id).ToListAsync(ct);
        var count = 0;
        foreach (var id in ids) if (await ProcessOneAsync(id, nowUtc, ct)) count++;
        return count;
    }
    private async Task<bool> ProcessOneAsync(int id, DateTime now, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var e = await db.CalendarEvents.Include(x => x.Client).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (e is null || e.BillingProcessed || e.ClientResponseStatus != AppointmentResponseStatus.Accepted || e.LawyerApprovalStatus is AppointmentApprovalStatus.Pending or AppointmentApprovalStatus.Rejected || e.EndDateTime > now || e.Status == EventStatus.Cancelled || e.AppointmentFee is not > 0 || e.ClientId is null) return false;
        var key = $"appointment:{e.Id}";
        if (await db.AppointmentBillingRecords.AnyAsync(x => x.IdempotencyKey == key, ct)) return false;
        var fee = e.AppointmentFee.Value;
        var record = new AppointmentBillingRecord { CalendarEventId = e.Id, IdempotencyKey = key, AppointmentFee = fee, Status = AppointmentBillingStatus.Pending };
        db.AppointmentBillingRecords.Add(record);
        var retainer = e.RetainerId is null ? null : await db.Retainers.SingleOrDefaultAsync(x => x.Id == e.RetainerId && x.ClientId == e.ClientId && x.Status == RetainerStatus.Active, ct);
        var trust = retainer is null ? null : await db.TrustAccounts.SingleOrDefaultAsync(x => x.ClientId == e.ClientId && !x.IsFrozen && !x.IsClosed, ct);
        int? clientUserId = notifications is null ? null : await db.Users.Where(x => x.Email == e.Client!.Email).Select(x => (int?)x.Id).SingleOrDefaultAsync(ct);
        Invoice invoice;
        if (retainer is not null && trust is not null && retainer.AvailableBalance >= fee && trust.Balance >= fee)
        {
            retainer.AvailableBalance -= fee;
            trust.Balance -= fee; trust.TotalWithdrawn += fee; trust.LastUpdated = now;
            var withdrawal = new TrustTransaction { TrustAccountId = trust.Id, Type = TransactionType.Withdrawal, Amount = fee, Description = $"Retainer #{retainer.Id} service charge for accepted appointment {e.Id}", Reference = $"APPT-{e.Id}", TransactionDate = now, AuthorizedBy = "Automatic retainer billing" };
            db.TrustTransactions.Add(withdrawal);
            invoice = CreateInvoice(e, fee, now, InvoiceStatus.Paid); invoice.IsPaid = true; invoice.PaidDate = now;
            db.Invoices.Add(invoice); await db.SaveChangesAsync(ct);
            record.TrustTransactionId = withdrawal.Id; record.CoveredAmount = fee; record.Status = AppointmentBillingStatus.TrustDeducted;
            var deductionText = $"Your accepted appointment \"{e.Title}\" has concluded. R{fee:N2} was deducted from retainer \"{retainer.Title}\" on {now:dd MMM yyyy}. Remaining retainer balance: R{retainer.AvailableBalance:N2}. Invoice: {invoice.InvoiceNumber}. Reference: {withdrawal.Reference}.";
            await email.QueueAsync(e.Client!.Email, "Service charge deducted from your retainer", $"<h2>Retainer charge confirmed</h2><p>{System.Net.WebUtility.HtmlEncode(deductionText)}</p><p><a href=\"{PublicUrl($"/Retainer/ClientRetainerDetails/{retainer.Id}")}\">View retainer</a></p>", deductionText, $"billing-retainer:{e.Id}", ct);
            if (clientUserId is not null && notifications is not null) await notifications.QueueAsync(clientUserId.Value, "RetainerDeduction", "Service charge deducted", deductionText, $"/Retainer/ClientRetainerDetails/{retainer.Id}", $"billing-retainer:{e.Id}", ct);
            var lowBalanceThreshold = Math.Max(fee, retainer!.Amount * .20m);
            if (retainer.AvailableBalance <= lowBalanceThreshold)
            {
                var lowBalanceText = $"Your retainer balance is low: R{retainer.AvailableBalance:N2} remains. Please top it up to avoid interruption to services.";
                await email.QueueAsync(e.Client.Email, "Low retainer balance", $"<h2>Low retainer balance</h2><p>{System.Net.WebUtility.HtmlEncode(lowBalanceText)}</p><p><a href=\"{PublicUrl($"/Retainer/DepositToRetainer/{retainer.Id}")}\">Top up retainer</a></p>", lowBalanceText, $"low-retainer:{retainer.Id}:{now:yyyyMM}", ct);
                if (clientUserId is not null && notifications is not null) await notifications.QueueAsync(clientUserId.Value, "LowRetainerBalance", "Low retainer balance", lowBalanceText, $"/Retainer/DepositToRetainer/{retainer.Id}", $"low-retainer:{retainer.Id}:{now:yyyyMM}", ct);
            }
            db.AuditEntries.Add(new() { EntityType = "CalendarEvent", EntityId = e.Id.ToString(), Action = "Trust account deducted",
                SafeMetadataJson = System.Text.Json.JsonSerializer.Serialize(new { amount = fee, invoice = invoice.InvoiceNumber, reference = withdrawal.Reference }) });
        }
        else
        {
            invoice = CreateInvoice(e, fee, now, InvoiceStatus.Sent); db.Invoices.Add(invoice); await db.SaveChangesAsync(ct);
            record.InvoicedAmount = fee; record.Status = AppointmentBillingStatus.Invoiced;
            var penalty = DescribePenalty(e);
            var balanceText = $"Your accepted appointment \"{e.Title}\" has concluded. Balance due: R{fee:N2}. Invoice: {invoice.InvoiceNumber}. Due date: {invoice.DueDate:dd MMM yyyy}. Please use the invoice number as your payment reference and follow the firm's normal payment instructions. {penalty}";
            await email.QueueAsync(e.Client!.Email, "Appointment balance due", $"<h2>Appointment payment due</h2><p>{System.Net.WebUtility.HtmlEncode(balanceText)}</p><p><a href=\"{PublicUrl($"/Billing/ClientInvoiceDetails/{invoice.Id}")}\">View invoice and payment instructions</a></p>", balanceText, $"billing-invoice:{e.Id}", ct);
            if (clientUserId is not null && notifications is not null) await notifications.QueueAsync(clientUserId.Value, "PaymentDue", "Payment due", balanceText, $"/Billing/InvoiceDetails/{invoice.Id}", $"billing-invoice:{e.Id}", ct);
            db.AuditEntries.Add(new() { EntityType = "Invoice", EntityId = invoice.Id.ToString(), Action = "Invoice created",
                SafeMetadataJson = System.Text.Json.JsonSerializer.Serialize(new { amount = fee, invoice = invoice.InvoiceNumber }) });
        }
        record.InvoiceId = invoice.Id; record.Status = AppointmentBillingStatus.Completed; record.CompletedAtUtc = now;
        e.GeneratedInvoiceId = invoice.Id; e.BillingProcessed = true; e.BillingProcessedAtUtc = now;
        db.AuditEntries.Add(new() { EntityType = "CalendarEvent", EntityId = e.Id.ToString(), Action = "Billing processed" });
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return true;
    }
    public async Task<int> ApplyPenaltiesAsync(DateTime now, CancellationToken ct = default)
    {
        var events = await db.CalendarEvents.Include(x => x.GeneratedInvoice).ThenInclude(x => x!.Payments).Include(x => x.Client)
            .Where(x => x.BillingProcessed && x.GeneratedInvoiceId != null && x.LatePenaltyType != LatePenaltyType.None && x.GeneratedInvoice!.Status != InvoiceStatus.Paid && x.GeneratedInvoice.Status != InvoiceStatus.Cancelled && x.GeneratedInvoice.DueDate.AddDays(x.LatePenaltyGraceDays) < now).ToListAsync(ct);
        var count = 0;
        foreach (var e in events)
        {
            var key = $"penalty-review:{e.Id}";
            if (await db.AuditEntries.AnyAsync(x => x.EntityType == "Invoice" && x.EntityId == e.GeneratedInvoiceId!.Value.ToString() && x.Action == key, ct)) continue;
            var invoice = e.GeneratedInvoice!;
            var paid = invoice.Payments?.Sum(x => x.Amount) ?? 0m;
            var outstanding = Math.Max(0, invoice.TotalAmount - paid);
            if (outstanding <= 0) continue;
            invoice.Status = InvoiceStatus.Overdue;
            db.AuditEntries.Add(new() { EntityType = "Invoice", EntityId = invoice.Id.ToString(), Action = key, SafeMetadataJson = System.Text.Json.JsonSerializer.Serialize(new { outstanding, invoice = invoice.InvoiceNumber }) });
            var accountants = await db.Users.Where(x => x.Role == UserRole.Accountant && x.IsActive).ToListAsync(ct);
            foreach (var accountant in accountants)
            {
                var text = $"Invoice {invoice.InvoiceNumber} is overdue by the agreed grace period. Review the account and record a factual reason before applying the pre-agreed penalty.";
                await email.QueueAsync(accountant.Email, "Penalty review required", $"<h2>Penalty review required</h2><p>{System.Net.WebUtility.HtmlEncode(text)}</p><p><a href=\"{PublicUrl("/Billing/PenaltyQueue")}\">Review overdue invoices</a></p>", text, $"penalty-review:{e.Id}:{accountant.Id}", ct);
                if (notifications is not null) await notifications.QueueAsync(accountant.Id, "PenaltyReview", "Penalty review required", text, "/Billing/PenaltyQueue", $"penalty-review:{e.Id}:{accountant.Id}", ct);
            }
            count++;
        }
        await db.SaveChangesAsync(ct); return count;
    }
    public async Task ApplyPenaltyAsync(int invoiceId, int accountantUserId, string reason, DateTime now, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 10) throw new InvalidOperationException("A specific penalty reason of at least 10 characters is required.");
        var accountant = await db.Users.SingleOrDefaultAsync(x => x.Id == accountantUserId && x.Role == UserRole.Accountant && x.IsActive, ct)
            ?? throw new UnauthorizedAccessException("Only an active accountant may apply a penalty.");
        var invoice = await db.Invoices.Include(x => x.Payments).Include(x => x.Client).SingleOrDefaultAsync(x => x.Id == invoiceId, ct)
            ?? throw new InvalidOperationException("Invoice not found.");
        var appointment = await db.CalendarEvents.SingleOrDefaultAsync(x => x.GeneratedInvoiceId == invoiceId, ct)
            ?? throw new InvalidOperationException("No agreed appointment penalty terms exist for this invoice.");
        if (invoice.Status != InvoiceStatus.Overdue || invoice.DueDate.AddDays(appointment.LatePenaltyGraceDays) >= now) throw new InvalidOperationException("The invoice is not eligible for a penalty.");
        if (appointment.LatePenaltyType == LatePenaltyType.None || appointment.LatePenaltyValue <= 0) throw new InvalidOperationException("No penalty was agreed for this service.");
        var key = $"appointment-penalty:{appointment.Id}";
        if (await db.InvoicePenalties.AnyAsync(x => x.IdempotencyKey == key, ct)) throw new InvalidOperationException("A penalty has already been applied.");
        var outstanding = Math.Max(0, invoice.TotalAmount - invoice.Payments.Sum(x => x.Amount));
        if (outstanding <= 0) throw new InvalidOperationException("The invoice has no outstanding balance.");
        var amount = appointment.LatePenaltyType == LatePenaltyType.FixedAmount ? appointment.LatePenaltyValue : decimal.Round(outstanding * appointment.LatePenaltyValue / 100m, 2);
        db.InvoicePenalties.Add(new() { InvoiceId = invoice.Id, CalendarEventId = appointment.Id, Type = appointment.LatePenaltyType, BasisAmount = outstanding, PenaltyValue = appointment.LatePenaltyValue, Amount = amount, AppliedAtUtc = now, IdempotencyKey = key, AppliedByAccountantId = accountant.Id, Reason = reason.Trim() });
        invoice.TotalAmount += amount;
        var text = $"A pre-agreed late payment penalty of R{amount:N2} was applied to invoice {invoice.InvoiceNumber}. Reason: {reason.Trim()}";
        await email.QueueAsync(invoice.Client!.Email, "Late payment penalty applied", $"<h2>Late payment penalty</h2><p>{System.Net.WebUtility.HtmlEncode(text)}</p><p><a href=\"{PublicUrl($"/Billing/ClientInvoiceDetails/{invoice.Id}")}\">View invoice</a></p>", text, $"penalty:{appointment.Id}", ct);
        var clientUserId = await db.Users.Where(x => x.Email == invoice.Client.Email).Select(x => (int?)x.Id).SingleOrDefaultAsync(ct);
        if (clientUserId is not null && notifications is not null) await notifications.QueueAsync(clientUserId.Value, "LatePaymentPenalty", "Late payment penalty applied", text, $"/Billing/ClientInvoiceDetails/{invoice.Id}", $"penalty:{appointment.Id}", ct);
        db.AuditEntries.Add(new() { ActorUserId = accountant.Id, EntityType = "Invoice", EntityId = invoice.Id.ToString(), Action = "Penalty applied by accountant", SafeMetadataJson = System.Text.Json.JsonSerializer.Serialize(new { amount, reason = reason.Trim(), terms = appointment.LatePenaltyType.ToString(), appointment.LatePenaltyValue }) });
        await db.SaveChangesAsync(ct);
    }
    private static Invoice CreateInvoice(CalendarEvent e, decimal fee, DateTime now, InvoiceStatus status) => new() { ClientId = e.ClientId!.Value, RetainerId = e.RetainerId, CaseId = e.CaseId, Amount = fee, TaxAmount = 0, TotalAmount = fee, IssueDate = now, DueDate = now.AddDays(e.PaymentDueDays), CreatedDate = now, CreatedAt = now, Status = status, InvoiceNumber = $"APT-{e.Id}-{now:yyyyMMdd}", Notes = $"Appointment: {e.Title}", Description = $"Accepted appointment fee: {e.Title}", Payments = [], TimeEntries = [] };
    private static string DescribePenalty(CalendarEvent e) => e.LatePenaltyType switch { LatePenaltyType.FixedAmount => $"A one-time late penalty of R{e.LatePenaltyValue:N2} may apply after {e.LatePenaltyGraceDays} grace day(s).", LatePenaltyType.Percentage => $"A one-time late penalty of {e.LatePenaltyValue:N2}% may apply after {e.LatePenaltyGraceDays} grace day(s).", _ => "No late penalty is configured." };
    private string PublicUrl(string path)
    {
        var root = configuration?["Email:PublicBaseUrl"]?.TrimEnd('/');
        return string.IsNullOrWhiteSpace(root) ? path : $"{root}{path}";
    }
}

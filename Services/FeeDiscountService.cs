using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.DTOs.FeeDiscount;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services.Notifications;

namespace SimplexLawFirm.Services
{
    public class FeeDiscountService : IFeeDiscountService
    {
        private readonly ApplicationDbContext _db;
        private readonly IAuditService _audit;
        private readonly INotificationService _notifications;

        public FeeDiscountService(
            ApplicationDbContext db,
            IAuditService audit,
            INotificationService notifications)
        {
            _db = db;
            _audit = audit;
            _notifications = notifications;
        }

        // ─────────────────────────────────────────────
        //  UC-05  Steps 2-9 : Client submits request
        // ─────────────────────────────────────────────
        public async Task<FeeDiscountRequestDetailDto> SubmitDiscountRequestAsync(
            int clientId, SubmitFeeDiscountRequestDto dto)
        {
            // ── Step 2: Retrieve the invoice ──
            var invoice = await _db.Invoices
                .Include(i => i.Case)
                .FirstOrDefaultAsync(i => i.Id == dto.InvoiceId && i.ClientId == clientId)
                ?? throw new InvalidOperationException("Invoice not found or does not belong to this client.");

            if (invoice.IsPaid)
                throw new InvalidOperationException("This invoice has already been paid.");

            // Calculate outstanding balance (invoice amount minus payments already made)
            var totalPaid = await _db.Payments
                .Where(p => p.InvoiceId == invoice.Id)
                .SumAsync(p => p.Amount);
            var outstandingBalance = invoice.Amount - totalPaid;

            // ── Step 3: Validate percentage / proposed amount ──
            if (dto.RequestedDiscountPercent is > 30m)
                throw new InvalidOperationException("The firm's maximum discount ceiling is 30%.");
            if (dto.RequestedDiscountPercent is > 0m)
                dto.ProposedAmount = decimal.Round(outstandingBalance * (1m - dto.RequestedDiscountPercent.Value / 100m), 2);
            if (dto.ProposedAmount <= 0)
                throw new InvalidOperationException("Proposed amount must be greater than zero.");
            if (dto.ProposedAmount >= outstandingBalance)
                throw new InvalidOperationException(
                    $"Proposed amount (R{dto.ProposedAmount:N2}) must be less than the outstanding balance (R{outstandingBalance:N2}).");

            // Check for existing pending requests on this invoice
            var existingPending = await _db.FeeDiscountRequests
                .AnyAsync(r => r.InvoiceId == invoice.Id
                    && (r.Status == FeeDiscountStatus.Pending
                        || r.Status == FeeDiscountStatus.PendingDirectorReview
                        || r.Status == FeeDiscountStatus.CounterOffered));
            if (existingPending)
                throw new InvalidOperationException("A discount request is already pending for this invoice.");

            // ── Step 4: Retrieve the minimum recovery threshold for the matter type ──
            var matterType = invoice.Case?.CaseType ?? "General";
            var policy = await _db.FeeDiscountPolicies
                .FirstOrDefaultAsync(p => p.MatterType == matterType);

            // Fall back to a "General" policy if the specific matter type has none
            policy ??= await _db.FeeDiscountPolicies
                .FirstOrDefaultAsync(p => p.MatterType == "General");

            decimal minRecoveryPercent = policy?.MinimumRecoveryPercent ?? 70m;
            decimal autoApprovalPercent = policy?.AutomaticApprovalPercent ?? 5m;

            // ── Step 5: Check minimum recovery threshold ──
            decimal proposedRecoveryPercent = (dto.ProposedAmount / outstandingBalance) * 100m;
            if (proposedRecoveryPercent < minRecoveryPercent)
                throw new InvalidOperationException(
                    $"The proposed amount recovers only {proposedRecoveryPercent:N1}% of the outstanding balance. " +
                    $"The minimum recovery threshold for {matterType} matters is {minRecoveryPercent:N1}%.");

            // ── Step 6: Retrieve payment history ──
            var (paidOnTime, overdueCount, totalBilled) = await GetPaymentHistoryAsync(clientId);

            // ── Step 7: Calculate discount percentage and evaluate reliability ──
            decimal discountPercent = ((outstandingBalance - dto.ProposedAmount) / outstandingBalance) * 100m;
            string reliability = ClassifyPaymentReliability(paidOnTime, overdueCount, totalBilled);
            var eligibility = await CalculateEligibilityAsync(clientId, paidOnTime, overdueCount, totalBilled);
            string recommendation = BuildRecommendation(discountPercent, eligibility.Limit, reliability);

            // ── Steps 8-9: Auto-approve or escalate ──
            bool canAutoApprove = discountPercent <= eligibility.Limit
                                  && reliability == PaymentReliability.Good;

            var request = new FeeDiscountRequest
            {
                InvoiceId = invoice.Id,
                ClientId = clientId,
                OriginalAmount = outstandingBalance,
                ProposedAmount = dto.ProposedAmount,
                DiscountPercent = discountPercent,
                Reason = dto.Reason,
                MinimumRecoveryPercentSnapshot = minRecoveryPercent,
                AutomaticApprovalPercentSnapshot = autoApprovalPercent,
                EligibilityScore = eligibility.Score,
                EligibilityTier = eligibility.Tier,
                EligibilityLimitSnapshot = eligibility.Limit,
                ThresholdCheckSnapshot = $"Pass: requested recovery {proposedRecoveryPercent:N2}% meets the {minRecoveryPercent:N2}% minimum for {matterType}.",
                SupportingDocumentPath = dto.SupportingDocumentPath,
                PaymentReliability = reliability,
                Recommendation = recommendation,
                Status = canAutoApprove
                    ? FeeDiscountStatus.AutoApproved
                    : FeeDiscountStatus.PendingDirectorReview,
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.FeeDiscountRequests.Add(request);

            // ── Step 8: If auto-approved, update the invoice immediately ──
            if (canAutoApprove)
            {
                request.DecidedAtUtc = DateTime.UtcNow;
                invoice.Amount = dto.ProposedAmount + totalPaid; // Revise total invoice amount
                await _audit.LogAsync("FeeDiscountRequest", request.Id.ToString(),
                    "AutoApproved",
                    $"Discount of {discountPercent:N2}% auto-approved. " +
                    $"Invoice revised from R{outstandingBalance:N2} to R{dto.ProposedAmount:N2}.",
                    actorUserId: null); // system action
            }

            await _db.SaveChangesAsync();

            // ── Step 11 (auto-approve path): Notify client ──
            if (canAutoApprove)
            {
                await QueueClientNotificationAsync(clientId,
                    "Fee Discount Approved",
                    $"Your discount request for invoice {invoice.InvoiceNumber} has been approved. " +
                    $"Your revised balance is R{dto.ProposedAmount:N2}.");
            }
            else
            {
                // Notify Director of escalation
                var directors = await _db.Users
                    .Where(u => u.Role == UserRole.Director && u.IsActive)
                    .Select(u => u.Id)
                    .ToListAsync();

                foreach (var dirId in directors)
                {
                    await _notifications.SendAsync(dirId,
                        "Fee Discount Escalated",
                        $"A fee discount request on invoice {invoice.InvoiceNumber} requires your review. " +
                        $"Discount: {discountPercent:N2}% | Reliability: {reliability} | Recommendation: {recommendation}");
                }
            }

            // ── Step 12: Audit log ──
            await _audit.LogAsync("FeeDiscountRequest", request.Id.ToString(),
                canAutoApprove ? "AutoApproved" : "EscalatedToDirector",
                $"Client requested {discountPercent:N2}% discount on invoice {invoice.InvoiceNumber}. " +
                $"Proposed: R{dto.ProposedAmount:N2}. Reliability: {reliability}.",
                actorUserId: clientId);

            return await MapToDetailDtoAsync(request.Id);
        }

        // ─────────────────────────────────────────────
        //  UC-05  Step 10 : Director decides
        // ─────────────────────────────────────────────
        public async Task<FeeDiscountRequestDetailDto> ProcessDirectorDecisionAsync(
            int directorUserId, DirectorFeeDiscountDecisionDto dto)
        {
            var request = await _db.FeeDiscountRequests
                .Include(r => r.Invoice)
                .FirstOrDefaultAsync(r => r.Id == dto.FeeDiscountRequestId)
                ?? throw new InvalidOperationException("Discount request not found.");

            if (request.Status != FeeDiscountStatus.PendingDirectorReview)
                throw new InvalidOperationException("This request is not pending Director review.");

            var invoice = request.Invoice
                ?? throw new InvalidOperationException("Linked invoice not found.");

            var totalPaid = await _db.Payments
                .Where(p => p.InvoiceId == invoice.Id)
                .SumAsync(p => p.Amount);

            switch (dto.Action)
            {
                case DirectorDecisionAction.Approve:
                    request.Status = FeeDiscountStatus.DirectorApproved;
                    request.DecidedByUserId = directorUserId;
                    request.DecidedAtUtc = DateTime.UtcNow;
                    request.DecisionReason = dto.DecisionReason ?? "Approved by Director.";

                    // Update invoice amount
                    invoice.Amount = request.ProposedAmount + totalPaid;

                    await QueueClientNotificationAsync(request.ClientId,
                        "Fee Discount Approved",
                        $"The Director has approved your discount request for invoice {invoice.InvoiceNumber}. " +
                        $"Your revised balance is R{request.ProposedAmount:N2}.");
                    break;

                case DirectorDecisionAction.Reject:
                    if (string.IsNullOrWhiteSpace(dto.DecisionReason))
                        throw new InvalidOperationException("A reason is required when rejecting a discount request.");

                    request.Status = FeeDiscountStatus.DirectorRejected;
                    request.DecidedByUserId = directorUserId;
                    request.DecidedAtUtc = DateTime.UtcNow;
                    request.DecisionReason = dto.DecisionReason;

                    await QueueClientNotificationAsync(request.ClientId,
                        "Fee Discount Rejected",
                        $"Your discount request for invoice {invoice.InvoiceNumber} has been declined. " +
                        $"Reason: {dto.DecisionReason}");
                    break;

                case DirectorDecisionAction.CounterOffer:
                    if (!dto.CounterOfferAmount.HasValue || dto.CounterOfferAmount.Value <= 0)
                        throw new InvalidOperationException("A valid counter-offer amount is required.");
                    if (string.IsNullOrWhiteSpace(dto.DecisionReason))
                        throw new InvalidOperationException("A reason is required when issuing a counter-offer.");

                    var outstanding = invoice.Amount - totalPaid;
                    if (dto.CounterOfferAmount.Value >= outstanding)
                        throw new InvalidOperationException("Counter-offer must be less than the outstanding balance.");

                    request.Status = FeeDiscountStatus.CounterOffered;
                    request.CounterOfferAmount = dto.CounterOfferAmount.Value;
                    request.DecidedByUserId = directorUserId;
                    request.DecisionReason = dto.DecisionReason;

                    await QueueClientNotificationAsync(request.ClientId,
                        "Fee Discount Counter-Offer",
                        $"The Director has proposed a counter-offer of R{dto.CounterOfferAmount.Value:N2} " +
                        $"for invoice {invoice.InvoiceNumber}. Please respond through the portal.");
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(dto.Action));
            }

            await _db.SaveChangesAsync();

            await _audit.LogAsync("FeeDiscountRequest", request.Id.ToString(),
                $"Director_{dto.Action}",
                $"Director decision on invoice {invoice.InvoiceNumber}: {dto.Action}. " +
                (dto.CounterOfferAmount.HasValue ? $"Counter-offer: R{dto.CounterOfferAmount.Value:N2}. " : "") +
                $"Reason: {dto.DecisionReason ?? "N/A"}",
                actorUserId: directorUserId);

            return await MapToDetailDtoAsync(request.Id);
        }

        // ─────────────────────────────────────────────
        //  Counter-offer response from client
        // ─────────────────────────────────────────────
        public async Task<FeeDiscountRequestDetailDto> RespondToCounterOfferAsync(
            int clientId, ClientCounterOfferResponseDto dto)
        {
            var request = await _db.FeeDiscountRequests
                .Include(r => r.Invoice)
                .FirstOrDefaultAsync(r => r.Id == dto.FeeDiscountRequestId && r.ClientId == clientId)
                ?? throw new InvalidOperationException("Discount request not found.");

            if (request.Status != FeeDiscountStatus.CounterOffered)
                throw new InvalidOperationException("This request does not have a pending counter-offer.");

            var invoice = request.Invoice!;
            var totalPaid = await _db.Payments
                .Where(p => p.InvoiceId == invoice.Id)
                .SumAsync(p => p.Amount);

            if (dto.Accept)
            {
                request.Status = FeeDiscountStatus.CounterOfferAccepted;
                request.DecidedAtUtc = DateTime.UtcNow;

                // Update invoice with the counter-offer amount
                invoice.Amount = request.CounterOfferAmount!.Value + totalPaid;

                // Notify Director
                if (request.DecidedByUserId.HasValue)
                {
                    await _notifications.SendAsync(request.DecidedByUserId.Value,
                        "Counter-Offer Accepted",
                        $"Client accepted the counter-offer of R{request.CounterOfferAmount:N2} " +
                        $"on invoice {invoice.InvoiceNumber}.");
                }
            }
            else
            {
                request.Status = FeeDiscountStatus.CounterOfferRejected;
                request.DecidedAtUtc = DateTime.UtcNow;

                if (request.DecidedByUserId.HasValue)
                {
                    await _notifications.SendAsync(request.DecidedByUserId.Value,
                        "Counter-Offer Rejected",
                        $"Client rejected the counter-offer on invoice {invoice.InvoiceNumber}. " +
                        $"The original balance remains.");
                }
            }

            await _db.SaveChangesAsync();

            await _audit.LogAsync("FeeDiscountRequest", request.Id.ToString(),
                dto.Accept ? "CounterOfferAccepted" : "CounterOfferRejected",
                $"Client {(dto.Accept ? "accepted" : "rejected")} counter-offer of " +
                $"R{request.CounterOfferAmount:N2} on invoice {invoice.InvoiceNumber}.",
                actorUserId: clientId);

            return await MapToDetailDtoAsync(request.Id);
        }

        // ─────────────────────────────────────────────
        //  Query methods
        // ─────────────────────────────────────────────
        public async Task<FeeDiscountRequestDetailDto?> GetByIdAsync(int id)
        {
            var exists = await _db.FeeDiscountRequests.AnyAsync(r => r.Id == id);
            return exists ? await MapToDetailDtoAsync(id) : null;
        }

        // Clients and authenticated users are distinct tables.  Never use a Client
        // primary key as a SystemNotification recipient: staff are seeded first, so
        // the numeric IDs do not reliably align.
        private async Task QueueClientNotificationAsync(int clientId, string title, string message)
        {
            var email = await _db.Clients.Where(c => c.Id == clientId).Select(c => c.Email).SingleOrDefaultAsync();
            if (string.IsNullOrWhiteSpace(email)) return;

            var userIds = await _db.Users
                .Where(u => u.Email == email && u.IsActive && u.Role == UserRole.Client)
                .Select(u => u.Id)
                .ToListAsync();
            foreach (var userId in userIds)
                await _notifications.SendAsync(userId, title, message);
        }

        private async Task<(decimal Score, string Tier, decimal Limit)> CalculateEligibilityAsync(int clientId, int paidOnTime, int overdueCount, decimal totalBilled)
        {
            var invoices = await _db.Invoices.Where(i => i.ClientId == clientId).ToListAsync();
            var payments = await _db.Payments.Where(p => p.Invoice!.ClientId == clientId).ToListAsync();
            var totalPaid = payments.Sum(p => p.Amount);
            var paymentCount = Math.Max(1, paidOnTime + overdueCount);
            var onTimeRate = (decimal)paidOnTime / paymentCount;
            var averageDaysToPay = invoices.Where(i => i.IsPaid && i.PaidDate.HasValue).Select(i => Math.Max(0, (i.PaidDate!.Value - i.IssueDate).Days)).DefaultIfEmpty(30).Average();
            var priorDiscounts = await _db.FeeDiscountRequests.CountAsync(r => r.ClientId == clientId && r.CreatedAtUtc >= DateTime.UtcNow.AddMonths(-12) && (r.Status == FeeDiscountStatus.AutoApproved || r.Status == FeeDiscountStatus.DirectorApproved || r.Status == FeeDiscountStatus.CounterOfferAccepted));
            var accountStanding = overdueCount == 0 ? 20m : overdueCount == 1 ? 10m : 0m;
            var score = decimal.Round(
                onTimeRate * 30m + (totalBilled <= 0m ? 10m : Math.Min(1m, totalPaid / totalBilled) * 20m) + accountStanding +
                Math.Max(0m, 10m - overdueCount * 3m) + Math.Max(0m, 10m - (decimal)averageDaysToPay / 6m) + Math.Max(0m, 10m - priorDiscounts * 3m), 2);
            score = Math.Clamp(score, 0m, 100m);
            var limit = score switch { < 50m => 0m, < 60m => 5m, < 70m => 10m, < 80m => 15m, < 90m => 20m, < 100m => 25m, _ => 30m };
            var tier = limit switch { 0m => "0–49", 5m => "50–59", 10m => "60–69", 15m => "70–79", 20m => "80–89", 25m => "90–99", _ => "100" };
            return (score, tier, limit);
        }

        public async Task<List<FeeDiscountRequestDetailDto>> GetByClientIdAsync(int clientId)
        {
            var ids = await _db.FeeDiscountRequests
                .Where(r => r.ClientId == clientId)
                .OrderByDescending(r => r.CreatedAtUtc)
                .Select(r => r.Id)
                .ToListAsync();

            var results = new List<FeeDiscountRequestDetailDto>();
            foreach (var id in ids)
                results.Add(await MapToDetailDtoAsync(id));
            return results;
        }

        public async Task<List<FeeDiscountRequestDetailDto>> GetPendingDirectorReviewAsync()
        {
            var ids = await _db.FeeDiscountRequests
                .Where(r => r.Status == FeeDiscountStatus.PendingDirectorReview)
                .OrderBy(r => r.CreatedAtUtc)
                .Select(r => r.Id)
                .ToListAsync();

            var results = new List<FeeDiscountRequestDetailDto>();
            foreach (var id in ids)
                results.Add(await MapToDetailDtoAsync(id));
            return results;
        }

        // ═════════════════════════════════════════════
        //  Private helpers
        // ═════════════════════════════════════════════

        /// <summary>UC-05 step 6: payment history for the client.</summary>
        private async Task<(int paidOnTime, int overdueCount, decimal totalBilled)>
            GetPaymentHistoryAsync(int clientId)
        {
            var invoices = await _db.Invoices
                .Where(i => i.ClientId == clientId)
                .ToListAsync();

            int paidOnTime = 0;
            int overdueCount = 0;
            decimal totalBilled = 0m;

            foreach (var inv in invoices)
            {
                totalBilled += inv.Amount;

                if (inv.IsPaid && inv.PaidDate.HasValue)
                {
                    if (inv.PaidDate.Value <= inv.DueDate)
                        paidOnTime++;
                    else
                        overdueCount++;
                }
                else if (!inv.IsPaid && inv.DueDate < DateTime.UtcNow)
                {
                    overdueCount++;
                }
            }

            return (paidOnTime, overdueCount, totalBilled);
        }

        /// <summary>UC-05 step 7: classify based on on-time rate.</summary>
        private static string ClassifyPaymentReliability(
            int paidOnTime, int overdueCount, decimal totalBilled)
        {
            int total = paidOnTime + overdueCount;
            if (total == 0) return PaymentReliability.Fair; // New client, no history

            decimal onTimeRate = (decimal)paidOnTime / total;

            return onTimeRate switch
            {
                >= 0.80m => PaymentReliability.Good,
                >= 0.50m => PaymentReliability.Fair,
                _ => PaymentReliability.Poor
            };
        }

        /// <summary>Build a human-readable recommendation for the Director.</summary>
        private static string BuildRecommendation(
            decimal discountPercent, decimal autoApprovalPercent, string reliability)
        {
            var parts = new List<string>();

            if (discountPercent <= autoApprovalPercent)
                parts.Add($"Discount of {discountPercent:N2}% is within the delegated approval limit of {autoApprovalPercent:N2}%.");
            else
                parts.Add($"Discount of {discountPercent:N2}% exceeds the delegated approval limit of {autoApprovalPercent:N2}%.");

            parts.Add($"Client payment reliability: {reliability}.");

            if (reliability == PaymentReliability.Good && discountPercent <= autoApprovalPercent)
                parts.Add("Recommendation: APPROVE. Low risk, within policy limits.");
            else if (reliability == PaymentReliability.Poor)
                parts.Add("Recommendation: REJECT or COUNTER-OFFER. Client has a poor payment track record.");
            else if (discountPercent > autoApprovalPercent * 2)
                parts.Add("Recommendation: REJECT or COUNTER-OFFER. Discount significantly exceeds policy limits.");
            else
                parts.Add("Recommendation: REVIEW. Discount is outside auto-approval parameters but may be justified.");

            return string.Join(" ", parts);
        }

        /// <summary>Map entity to full detail DTO.</summary>
        private async Task<FeeDiscountRequestDetailDto> MapToDetailDtoAsync(int requestId)
        {
            var r = await _db.FeeDiscountRequests
                .Include(x => x.Invoice).ThenInclude(i => i!.Case)
                .Include(x => x.Client)
                .FirstAsync(x => x.Id == requestId);

            var decidedBy = r.DecidedByUserId.HasValue
                ? await _db.Users.FindAsync(r.DecidedByUserId.Value)
                : null;

            var (paidOnTime, overdueCount, totalBilled) = await GetPaymentHistoryAsync(r.ClientId);

            return new FeeDiscountRequestDetailDto
            {
                Id = r.Id,
                InvoiceId = r.InvoiceId,
                InvoiceNumber = r.Invoice?.InvoiceNumber ?? "",
                ClientId = r.ClientId,
                ClientName = r.Client != null
                    ? (r.Client.IsBusiness
                        ? r.Client.CompanyName ?? $"{r.Client.FirstName} {r.Client.LastName}"
                        : $"{r.Client.FirstName} {r.Client.LastName}")
                    : "",
                MatterType = r.Invoice?.Case?.CaseType ?? "General",
                OriginalAmount = r.OriginalAmount,
                ProposedAmount = r.ProposedAmount,
                DiscountPercent = r.DiscountPercent,
                CounterOfferAmount = r.CounterOfferAmount,
                MinimumRecoveryPercentSnapshot = r.MinimumRecoveryPercentSnapshot,
                AutomaticApprovalPercentSnapshot = r.AutomaticApprovalPercentSnapshot,
                EligibilityScore = r.EligibilityScore,
                EligibilityTier = r.EligibilityTier,
                EligibilityLimitSnapshot = r.EligibilityLimitSnapshot,
                ThresholdCheckSnapshot = r.ThresholdCheckSnapshot,
                SupportingDocumentPath = r.SupportingDocumentPath,
                PaymentReliability = r.PaymentReliability,
                Recommendation = r.Recommendation,
                Reason = r.Reason,
                Status = ((FeeDiscountStatus)r.Status).ToString(),
                DecisionReason = r.DecisionReason,
                DecidedByName = decidedBy?.FullName,
                DecidedAtUtc = r.DecidedAtUtc,
                InvoicesPaidOnTime = paidOnTime,
                InvoicesOverdue = overdueCount,
                TotalValueBilled = totalBilled,
                CreatedAtUtc = r.CreatedAtUtc
            };
        }
    }
}

namespace SimplexLawFirm.Models
{

    public class RetainerDashboardViewModel
    {
        public int PendingRequestsCount { get; set; }
        public int PendingApprovalCount { get; set; }
        public int ActiveRetainersCount { get; set; }
        public int AwaitingSignatureCount { get; set; }
        public int AwaitingPaymentCount { get; set; }
        public List<ClientRequest> RecentRequests { get; set; }
        public List<Retainer> RecentRetainers { get; set; }
        public decimal TotalTrustBalance { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public List<OverduePaymentInfo> OverduePayments { get; set; }
        public int DraftCount { get; set; }
    }

    public class ClientRetainerDetailsViewModel
    {
        public Retainer Retainer { get; set; }
        public Invoice Invoice { get; set; }
        public List<Payment> Payments { get; set; }
        public List<RetainerPaymentSchedule> PaymentSchedules { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal RemainingBalance { get; set; }
        public List<RetainerActionLog> ActionLogs { get; set; }
    }

    public class ClientRequestViewModel
    {
        public ClientRequest Request { get; set; }
        public RetainerTemplate Template { get; set; }
        public Client Client { get; set; }
        public List<RetainerTemplate> AvailableTemplates { get; set; }
    }

    public class RetainerApprovalViewModel
    {
        public Retainer Retainer { get; set; }
        public Client Client { get; set; }
        public RetainerTemplate Template { get; set; }
        public Case AssociatedCase { get; set; }
    }
}
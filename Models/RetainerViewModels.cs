using System.ComponentModel.DataAnnotations;

namespace SimplexLawFirm.Models
{
    public class RetainerViewModels
    {
    }





    public class LawyerReviewViewModel
    {
        public Retainer Retainer { get; set; }
        public List<RetainerActionLog> ActionLogs { get; set; }
        public string SuggestedChanges { get; set; }
    }

    public class RetainerSignViewModel
    {
        public Retainer Retainer { get; set; }
        public bool RequiresPayment { get; set; }
        public decimal PaymentAmount { get; set; }
        public List<RetainerPaymentSchedule> PaymentSchedules { get; set; }
        public Invoice Invoice { get; set; }
        public bool HasOutstandingInvoice { get; set; }
    }

    public class RetainerPaymentViewModel
    {
        public int RetainerId { get; set; }
        public string RetainerTitle { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal RemainingBalance { get; set; }
        public string SignatureToken { get; set; }
        public Invoice Invoice { get; set; }
        public List<RetainerPaymentSchedule> PaymentSchedules { get; set; }
    }

 

    

   

    public class OverduePaymentInfo
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; }
        public string ClientName { get; set; }
        public decimal AmountDue { get; set; }
        public DateTime DueDate { get; set; }
        public int DaysOverdue { get; set; }
    }

    

   

   
}
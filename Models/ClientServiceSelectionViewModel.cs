// Models/RetainerViewModels.cs
namespace SimplexLawFirm.Models
{
    public class ClientServiceSelectionViewModel
    {
        public int TemplateId { get; set; }
        public RetainerTemplate? Template { get; set; }
        public int ClientId { get; set; }
        public Client? Client { get; set; }
        public string? ClientNotes { get; set; }
    }


    public class RetainerDetailsViewModel
    {
        public Retainer? Retainer { get; set; }
        public Invoice? Invoice { get; set; }
        public List<Payment> Payments { get; set; } = new List<Payment>();
        public List<RetainerPaymentSchedule> PaymentSchedules { get; set; }
        public decimal TotalPaid { get; set; }
        public bool CanEdit { get; set; }
        public bool CanSubmit { get; set; }
        public bool CanApprove { get; set; }
        public List<RetainerActionLog> ActionLogs { get; set; }

        

    }



   

   

    
}
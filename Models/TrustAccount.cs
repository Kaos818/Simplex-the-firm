using System.ComponentModel.DataAnnotations;

namespace SimplexLawFirm.Models
{
    public class TrustAccount
    {
        public int Id { get; set; }

        [Required]
        public int ClientId { get; set; }
        public Client? Client { get; set; }

        [DataType(DataType.Currency)]
        public decimal Balance { get; set; }

        [DataType(DataType.Currency)]
        public decimal TotalDeposited { get; set; }

        [DataType(DataType.Currency)]
        public decimal TotalWithdrawn { get; set; }

        public bool IsFrozen { get; set; }
        public bool IsClosed { get; set; }

        public DateTime LastUpdated { get; set; }

        public ICollection<TrustTransaction> Transactions { get; set; }
    }

    public class TrustTransaction
    {
        public int Id { get; set; }

        [Required]
        public int TrustAccountId { get; set; }
        public TrustAccount? TrustAccount { get; set; }

        public TransactionType Type { get; set; }

        [DataType(DataType.Currency)]
        public decimal Amount { get; set; }

        public string? Description { get; set; }

        public string? Reference { get; set; }

        public DateTime TransactionDate { get; set; }

        public int? RelatedInvoiceId { get; set; }

        public string? AuthorizedBy { get; set; }
    }

    public enum InvoiceStatus
    {
        Draft,
        Sent,
        Paid,
        Overdue,
        Cancelled,
        PartiallyPaid
    }

    public enum PaymentMethod
    {
        Cash,
        BankTransfer,
        CreditCard,
        DebitCard,
        Cheque,
        EFT
    }

    public enum TransactionType
    {
        Deposit,
        Withdrawal,
        Transfer
    }
}

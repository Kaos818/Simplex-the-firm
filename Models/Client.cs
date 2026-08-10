namespace SimplexLawFirm.Models
{
    public class Client
    {
        public int Id { get; set; }
        public bool IsBusiness { get; set; }

        // Individual
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? SAIDNumber { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;


        // Business
        public string? CompanyName { get; set; }
        public string? RegistrationNumber { get; set; }

        public string FullName => IsBusiness ? CompanyName : $"{FirstName} {LastName}";

        public string Email { get; set; }
        public string Phone { get; set; }

        public string? IdentificationNumber { get; set; } 

        public ICollection<Case> Cases { get; set; }
    }
}

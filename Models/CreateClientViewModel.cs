using System.ComponentModel.DataAnnotations;

namespace SimplexLawFirm.Models
{
    public class CreateClientViewModel
    {
        public bool IsBusiness { get; set; }

        // Individual
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? SAIDNumber { get; set; }

        // Business
        public string? CompanyName { get; set; }
        public string? RegistrationNumber { get; set; }

        // Common
        [Required]
        public string Email { get; set; }

        [Required]
        public string Phone { get; set; }

        public string? Password { get; set; }
    }

    public class CreateUserViewModel
    {
        [Required]
        public string FullName { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public UserRole Role { get; set; }

        public string? Password { get; set; }

        public bool IsActive { get; set; } = true;
    }
}

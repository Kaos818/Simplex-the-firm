using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimplexLawFirm.Models
{
    public class LawyerProfile
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        [Display(Name = "Bar Number")]
        [MaxLength(50)]
        public string BarNumber { get; set; }

        [Display(Name = "Hourly Rate")]
        [DataType(DataType.Currency)]
        public decimal HourlyRate { get; set; }

        [Display(Name = "Bio")]
        [DataType(DataType.MultilineText)]
        [MaxLength(2000)]
        public string Bio { get; set; }

        [Display(Name = "Office Location")]
        [MaxLength(200)]
        public string OfficeLocation { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Years of Experience")]
        public int YearsOfExperience { get; set; }

        // This is the correct relationship - many-to-many
        public ICollection<LawyerSpecialization> Specializations { get; set; }
    }
}

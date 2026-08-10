using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimplexLawFirm.Models
{
    public class LawyerSpecialization
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Area of Law")]
        [MaxLength(100)]
        public string Name { get; set; }

        [Display(Name = "Description")]
        [DataType(DataType.MultilineText)]
        [MaxLength(500)]
        public string Description { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
        
        [InverseProperty("Specializations")]
        public ICollection<LawyerProfile> LawyerProfiles { get; set; }
    }
}
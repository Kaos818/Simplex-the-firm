using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimplexLawFirm.Models
{
    public class TaskItem
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public TaskPriority Priority { get; set; }

        public TaskStatus Status { get; set; }

        public DateTime? DueDate { get; set; }

        [Display(Name = "Assigned To")]
        public int? AssignedToId { get; set; }

        [ForeignKey("AssignedToId")]
        public ApplicationUser AssignedTo { get; set; }

        public DateTime? CompletedAt { get; set; }

        public int? CreatedById { get; set; }
        public ApplicationUser CreatedBy { get; set; }

        public int? CaseId { get; set; }
        public Case Case { get; set; }

        public int? RetainerId { get; set; }
        public Retainer Retainer { get; set; }

        public int? ParentTaskId { get; set; }

        [Display(Name = "Related Event")]
        public int? RelatedEventId { get; set; }

        [ForeignKey("RelatedEventId")]
        public CalendarEvent RelatedEvent { get; set; }

        [ForeignKey("ParentTaskId")]
        public TaskItem ParentTask { get; set; }

        [Display(Name = "Estimated Hours")]
        public decimal? EstimatedHours { get; set; }

        [Display(Name = "Actual Hours")]
        public decimal? ActualHours { get; set; }

        [Display(Name = "Is Billable")]
        public bool IsBillable { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }

        public ICollection<TaskComment> Comments { get; set; }
        public ICollection<TaskAttachment> Attachments { get; set; }
        public ICollection<TaskItem> SubTasks { get; set; }
    }

}

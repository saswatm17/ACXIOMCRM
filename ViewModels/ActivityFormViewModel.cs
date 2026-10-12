using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Models.Constants;

namespace AcxiomCRM.ViewModels
{
    public class ActivityFormViewModel : IValidatableObject
    {
        public int ActivityId { get; set; }

        [Required(ErrorMessage = "Activity type is required.")]
        [StringLength(50)]
        [Display(Name = "Activity Type")]
        public string ActivityType { get; set; } = AcxiomCRM.Models.Constants.ActivityType.Call;

        [Required(ErrorMessage = "Subject is required.")]
        [StringLength(150, ErrorMessage = "Subject cannot exceed 150 characters.")]
        [Display(Name = "Activity Subject")]
        public string Subject { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        [Display(Name = "Description & Outcome Notes")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Activity date and time is required.")]
        [Display(Name = "Activity Date & Time")]
        public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Related Customer")]
        public int? CustomerId { get; set; }

        [Display(Name = "Related Lead")]
        public int? LeadId { get; set; }

        [StringLength(30)]
        [Display(Name = "Status")]
        public string Status { get; set; } = ActivityStatus.Pending;

        [Display(Name = "Assigned Team Member")]
        public string? AssignedTo { get; set; }

        public IEnumerable<SelectListItem> CustomersList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> LeadsList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> UsersList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> TypesList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> StatusesList { get; set; } = new List<SelectListItem>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!CustomerId.HasValue && !LeadId.HasValue)
            {
                yield return new ValidationResult(
                    "Please associate this activity with either a Customer or a Lead.",
                    new[] { nameof(CustomerId) });
            }
        }
    }
}

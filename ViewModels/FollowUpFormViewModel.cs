using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Models.Constants;

namespace AcxiomCRM.ViewModels
{
    public class FollowUpFormViewModel : IValidatableObject
    {
        public int FollowUpId { get; set; }

        [Display(Name = "Related Customer")]
        public int? CustomerId { get; set; }

        [Display(Name = "Related Lead")]
        public int? LeadId { get; set; }

        [Required(ErrorMessage = "Follow-up date and time is required.")]
        [AcxiomCRM.Common.ValidationAttributes.FutureDate("Follow-up date cannot be earlier than today.")]
        [Display(Name = "Follow-up Date & Time")]
        public DateTime FollowUpDate { get; set; } = DateTime.UtcNow.AddDays(1);

        [Required(ErrorMessage = "Follow-up type is required.")]
        [StringLength(50)]
        [Display(Name = "Follow-up Type")]
        public string FollowUpType { get; set; } = AcxiomCRM.Models.Constants.FollowUpType.Call;

        [StringLength(500, ErrorMessage = "Remarks cannot exceed 500 characters.")]
        [Display(Name = "Notes / Agenda")]
        public string? Remarks { get; set; }

        [StringLength(30)]
        [Display(Name = "Status")]
        public string Status { get; set; } = FollowUpStatus.Planned;

        [Display(Name = "Assigned Team Member")]
        public string? AssignedTo { get; set; }

        public IEnumerable<SelectListItem> CustomersList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> LeadsList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> UsersList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> TypesList { get; set; } = new List<SelectListItem>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (FollowUpId == 0 && FollowUpDate.Date < DateTime.UtcNow.Date)
            {
                yield return new ValidationResult(
                    "Follow-up date cannot be earlier than today.",
                    new[] { nameof(FollowUpDate) });
            }

            if (!CustomerId.HasValue && !LeadId.HasValue)
            {
                yield return new ValidationResult(
                    "Please associate this follow-up with either a Customer or a Lead.",
                    new[] { nameof(CustomerId) });
            }
        }
    }
}

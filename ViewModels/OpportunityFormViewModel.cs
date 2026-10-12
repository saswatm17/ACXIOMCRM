using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Models.Constants;

namespace AcxiomCRM.ViewModels
{
    public class OpportunityFormViewModel : IValidatableObject
    {
        public int OpportunityId { get; set; }

        [Required(ErrorMessage = "Opportunity name is required.")]
        [StringLength(100, ErrorMessage = "Opportunity name cannot exceed 100 characters.")]
        [Display(Name = "Opportunity Name")]
        public string OpportunityName { get; set; } = string.Empty;

        [Display(Name = "Related Customer")]
        public int? CustomerId { get; set; }

        [Display(Name = "Originating Lead")]
        public int? LeadId { get; set; }

        [Required(ErrorMessage = "Amount is required.")]
        [Range(0.01, 1000000000, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        [Display(Name = "Amount (₹)")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Stage is required.")]
        [StringLength(50)]
        [Display(Name = "Pipeline Stage")]
        public string Stage { get; set; } = OpportunityStage.Qualification;

        [Required(ErrorMessage = "Probability is required.")]
        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        [Display(Name = "Probability (%)")]
        public int Probability { get; set; } = 20;

        [Required(ErrorMessage = "Expected close date is required.")]
        [AcxiomCRM.Common.ValidationAttributes.FutureDate("Expected Close Date cannot be in the past.")]
        [DataType(DataType.Date)]
        [Display(Name = "Expected Close Date")]
        public DateTime ExpectedCloseDate { get; set; } = DateTime.UtcNow.AddDays(30);

        [StringLength(30)]
        public string Status { get; set; } = OpportunityStatus.Open;

        [Display(Name = "Assigned Sales Executive")]
        public string? AssignedTo { get; set; }

        public IEnumerable<SelectListItem> CustomersList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> LeadsList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> UsersList { get; set; } = new List<SelectListItem>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Business rule: Expected Close Date cannot be in the past for active opportunities
            if (Stage != OpportunityStage.Won && Stage != OpportunityStage.Lost)
            {
                if (ExpectedCloseDate.Date < DateTime.UtcNow.Date)
                {
                    yield return new ValidationResult(
                        "Expected Close Date cannot be in the past.",
                        new[] { nameof(ExpectedCloseDate) });
                }
            }

            if (Amount <= 0)
            {
                yield return new ValidationResult(
                    "Opportunity Amount must be greater than 0.",
                    new[] { nameof(Amount) });
            }
        }
    }
}

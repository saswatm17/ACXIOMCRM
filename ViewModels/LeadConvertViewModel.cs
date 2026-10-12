using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models.Constants;

namespace AcxiomCRM.ViewModels
{
    public class LeadConvertViewModel
    {
        public int LeadId { get; set; }
        public string LeadCode { get; set; } = string.Empty;
        public string LeadName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? CompanyName { get; set; }

        [Required(ErrorMessage = "Opportunity Name is required.")]
        [StringLength(100, ErrorMessage = "Opportunity Name cannot exceed 100 characters.")]
        [Display(Name = "Opportunity Name")]
        public string OpportunityName { get; set; } = string.Empty;

        [Range(0.01, 1000000000, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        [Display(Name = "Deal Amount (₹)")]
        public decimal DealAmount { get; set; }

        [Required(ErrorMessage = "Expected close date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Expected Close Date")]
        public DateTime ExpectedCloseDate { get; set; } = DateTime.UtcNow.AddDays(30);

        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        [Display(Name = "Initial Probability (%)")]
        public int Probability { get; set; } = 25;

        [Required]
        [Display(Name = "Pipeline Stage")]
        public string Stage { get; set; } = OpportunityStage.Proposal;

        [StringLength(500)]
        [Display(Name = "Conversion Notes")]
        public string? Notes { get; set; }
    }
}

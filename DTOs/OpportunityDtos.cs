using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Common.ValidationAttributes;

namespace AcxiomCRM.DTOs
{
    public class OpportunityReadDto
    {
        public int OpportunityId { get; set; }
        public string OpportunityName { get; set; } = string.Empty;
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int? LeadId { get; set; }
        public string? LeadName { get; set; }
        public decimal Amount { get; set; }
        public string Stage { get; set; } = string.Empty;
        public int Probability { get; set; }
        public decimal WeightedAmount { get; set; }
        public DateTime ExpectedCloseDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? AssignedToUserId { get; set; }
        public string? AssignedToName { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class OpportunityCreateDto
    {
        [Required(ErrorMessage = "Opportunity name is required.")]
        [StringLength(100, ErrorMessage = "Opportunity name cannot exceed 100 characters.")]
        public string OpportunityName { get; set; } = string.Empty;

        public int? CustomerId { get; set; }
        public int? LeadId { get; set; }

        [Required(ErrorMessage = "Amount is required.")]
        [Range(0.01, 1000000000, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Stage is required.")]
        [StringLength(50)]
        public string Stage { get; set; } = "Qualification";

        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        public int Probability { get; set; } = 25;

        [Required(ErrorMessage = "Expected close date is required.")]
        [FutureDate(ErrorMessage = "Expected Close Date cannot be in the past.")]
        public DateTime ExpectedCloseDate { get; set; } = DateTime.UtcNow.AddDays(30);

        public string? AssignedTo { get; set; }
    }

    public class OpportunityUpdateDto
    {
        [Required(ErrorMessage = "Opportunity name is required.")]
        [StringLength(100, ErrorMessage = "Opportunity name cannot exceed 100 characters.")]
        public string OpportunityName { get; set; } = string.Empty;

        public int? CustomerId { get; set; }
        public int? LeadId { get; set; }

        [Required(ErrorMessage = "Amount is required.")]
        [Range(0.01, 1000000000, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Stage is required.")]
        [StringLength(50)]
        public string Stage { get; set; } = "Qualification";

        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        public int Probability { get; set; } = 25;

        [Required(ErrorMessage = "Expected close date is required.")]
        public DateTime ExpectedCloseDate { get; set; }

        public string? AssignedTo { get; set; }
    }

    public class ChangeStageApiDto
    {
        [Required(ErrorMessage = "Stage is required.")]
        public string Stage { get; set; } = string.Empty;

        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        public int? Probability { get; set; }
    }
}

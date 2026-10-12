using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Common.ValidationAttributes;

namespace AcxiomCRM.DTOs
{
    public class LeadReadDto
    {
        public int LeadId { get; set; }
        public string LeadCode { get; set; } = string.Empty;
        public string LeadName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal ExpectedValue { get; set; }
        public string? AssignedToUserId { get; set; }
        public string? AssignedToName { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class LeadCreateDto
    {
        [Required(ErrorMessage = "Lead name is required.")]
        [StringLength(100, ErrorMessage = "Lead name cannot exceed 100 characters.")]
        public string LeadName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [IndianPhone(ErrorMessage = "Enter a valid phone number.")]
        [StringLength(15)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(100)]
        public string? CompanyName { get; set; }

        [Required(ErrorMessage = "Lead source is required.")]
        [StringLength(50)]
        public string Source { get; set; } = "Website";

        [Range(0, 100000000, ErrorMessage = "Expected value must be a valid positive amount.")]
        public decimal ExpectedValue { get; set; }

        public string? AssignedTo { get; set; }
    }

    public class LeadUpdateDto
    {
        [Required(ErrorMessage = "Lead name is required.")]
        [StringLength(100, ErrorMessage = "Lead name cannot exceed 100 characters.")]
        public string LeadName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [IndianPhone(ErrorMessage = "Enter a valid phone number.")]
        [StringLength(15)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(100)]
        public string? CompanyName { get; set; }

        [Required(ErrorMessage = "Lead source is required.")]
        [StringLength(50)]
        public string Source { get; set; } = "Website";

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "New";

        [Range(0, 100000000, ErrorMessage = "Expected value must be a valid positive amount.")]
        public decimal ExpectedValue { get; set; }

        public string? AssignedTo { get; set; }
    }

    public class LeadConvertApiDto
    {
        [Required(ErrorMessage = "Opportunity deal name is required.")]
        [StringLength(100)]
        public string OpportunityName { get; set; } = string.Empty;

        [Range(0.01, 1000000000, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        public decimal OpportunityAmount { get; set; }

        [FutureDate(ErrorMessage = "Expected Close Date cannot be in the past.")]
        public DateTime ExpectedCloseDate { get; set; } = DateTime.UtcNow.AddDays(30);

        public string? AssignedTo { get; set; }
    }
}

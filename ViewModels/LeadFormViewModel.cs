using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Models.Constants;

namespace AcxiomCRM.ViewModels
{
    public class LeadFormViewModel
    {
        public int LeadId { get; set; }

        [Display(Name = "Lead Code")]
        public string? LeadCode { get; set; }

        [Required(ErrorMessage = "Lead name is required.")]
        [StringLength(100, ErrorMessage = "Lead name cannot exceed 100 characters.")]
        [Display(Name = "Lead Name")]
        public string LeadName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(100)]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [AcxiomCRM.Common.ValidationAttributes.IndianPhone(ErrorMessage = "Enter a valid phone number.")]
        [StringLength(15)]
        [Display(Name = "Mobile Number (10 digits)")]
        public string Phone { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [Required(ErrorMessage = "Lead source is required.")]
        [StringLength(50)]
        [Display(Name = "Lead Source")]
        public string Source { get; set; } = LeadSource.Website;

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = LeadStatus.New;

        [Range(0, 100000000, ErrorMessage = "Expected value must be a valid positive amount.")]
        [Display(Name = "Expected Value (₹)")]
        public decimal ExpectedValue { get; set; }

        [Display(Name = "Assign To Sales Executive")]
        public string? AssignedTo { get; set; }

        public IEnumerable<SelectListItem> UsersList { get; set; } = new List<SelectListItem>();
    }
}

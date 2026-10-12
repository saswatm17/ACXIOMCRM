using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models.Constants;

namespace AcxiomCRM.ViewModels
{
    public class CustomerFormViewModel
    {
        public int CustomerId { get; set; }

        [Display(Name = "Customer Code")]
        public string? CustomerCode { get; set; }

        [Required(ErrorMessage = "Customer name is required.")]
        [StringLength(100, ErrorMessage = "Customer name cannot exceed 100 characters.")]
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(100)]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [AcxiomCRM.Common.ValidationAttributes.IndianPhone(ErrorMessage = "Enter a valid phone number.")]
        [StringLength(15)]
        [Display(Name = "Phone Number (10 digits)")]
        public string Phone { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        [StringLength(50)]
        public string? City { get; set; }

        [StringLength(50)]
        public string? State { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = CustomerStatus.Active;
    }
}

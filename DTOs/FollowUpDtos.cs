using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Common.ValidationAttributes;

namespace AcxiomCRM.DTOs
{
    public class FollowUpReadDto
    {
        public int FollowUpId { get; set; }
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int? LeadId { get; set; }
        public string? LeadName { get; set; }
        public DateTime FollowUpDate { get; set; }
        public string FollowUpType { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? AssignedToUserId { get; set; }
        public string? AssignedToName { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class FollowUpCreateDto
    {
        public int? CustomerId { get; set; }
        public int? LeadId { get; set; }

        [Required(ErrorMessage = "Follow-up date and time is required.")]
        [FutureDate(ErrorMessage = "Follow-up date cannot be earlier than today.")]
        public DateTime FollowUpDate { get; set; }

        [Required(ErrorMessage = "Follow-up type is required.")]
        [StringLength(50)]
        public string FollowUpType { get; set; } = "Call";

        [StringLength(500)]
        public string? Remarks { get; set; }

        public string? AssignedTo { get; set; }
    }

    public class FollowUpCompleteDto
    {
        [StringLength(500)]
        public string? Remarks { get; set; }
    }

    public class FollowUpRescheduleDto
    {
        [Required(ErrorMessage = "New date is required.")]
        [FutureDate(ErrorMessage = "Follow-up date cannot be earlier than today.")]
        public DateTime NewDate { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }
    }
}

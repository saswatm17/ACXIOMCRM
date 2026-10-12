using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models
{
    public class AuditLog
    {
        [Key]
        public int AuditLogId { get; set; }

        [StringLength(450)]
        [Display(Name = "User ID")]
        public string? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser? User { get; set; }

        [Required]
        [StringLength(50)]
        public string Action { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Entity Name")]
        public string EntityName { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Record ID")]
        public string? RecordId { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        [Display(Name = "Old Values")]
        public string? OldValue { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        [Display(Name = "New Values")]
        public string? NewValue { get; set; }

        [Display(Name = "Timestamp")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        [Display(Name = "IP Address")]
        public string? IpAddress { get; set; }
    }
}

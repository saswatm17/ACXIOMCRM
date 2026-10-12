using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels
{
    public class AuditLogListViewModel
    {
        public IEnumerable<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

        public string? ActionFilter { get; set; }
        public string? EntityFilter { get; set; }
        public string? UserFilter { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SearchTerm { get; set; }

        public int TotalLogsCount { get; set; }
        public int CreatesCount { get; set; }
        public int UpdatesCount { get; set; }
        public int SecurityEventsCount { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public int TotalPages { get; set; } = 1;
        public int TotalItems { get; set; }

        public IEnumerable<SelectListItem> ActionsList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> EntitiesList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> UsersList { get; set; } = new List<SelectListItem>();
    }

    public class PropertyDiffItem
    {
        public string PropertyName { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public bool IsChanged { get; set; }
    }

    public class AuditLogDetailsViewModel
    {
        public AuditLog Log { get; set; } = null!;
        public string? FormattedOldJson { get; set; }
        public string? FormattedNewJson { get; set; }
        public List<PropertyDiffItem> PropertyDiffs { get; set; } = new List<PropertyDiffItem>();
    }
}

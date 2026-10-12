using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.ViewModels
{
    public class ReportIndexItem
    {
        public string ActionName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string BadgeColor { get; set; } = "primary";
    }

    public class ReportFilterViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? StaffId { get; set; }
        public string? Status { get; set; }
        public string? Stage { get; set; }
        public string? State { get; set; }

        public IEnumerable<SelectListItem> StaffList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> StatusList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> StageList { get; set; } = new List<SelectListItem>();
    }

    // 1. Lead Conversion Report
    public class LeadConversionReportItem
    {
        public string LeadCode { get; set; } = string.Empty;
        public string LeadName { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public bool IsConverted { get; set; }
        public string? CustomerName { get; set; }
        public decimal? DealAmount { get; set; }
        public string? AssignedToStaff { get; set; }
    }

    // 2. Sales Pipeline Report
    public class SalesPipelineReportItem
    {
        public int OpportunityId { get; set; }
        public string OpportunityName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Stage { get; set; } = string.Empty;
        public int Probability { get; set; }
        public decimal Amount { get; set; }
        public decimal WeightedAmount { get; set; }
        public DateTime ExpectedCloseDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string AssignedToStaff { get; set; } = string.Empty;
    }

    // 3. Sales Performance Report
    public class SalesPerformanceReportItem
    {
        public string StaffId { get; set; } = string.Empty;
        public string StaffName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int TotalDeals { get; set; }
        public int WonDealsCount { get; set; }
        public decimal TotalPipelineValue { get; set; }
        public decimal TotalWonRevenue { get; set; }
        public double WinRate { get; set; }
    }

    // 4. Activity Summary Report
    public class ActivitySummaryReportItem
    {
        public string StaffId { get; set; } = string.Empty;
        public string StaffName { get; set; } = string.Empty;
        public int TotalActivities { get; set; }
        public int CallsCount { get; set; }
        public int MeetingsCount { get; set; }
        public int EmailsCount { get; set; }
        public int TasksCount { get; set; }
        public int CompletedCount { get; set; }
        public double CompletionRate { get; set; }
    }

    // 5. Follow-Up Compliance Report
    public class FollowUpComplianceReportItem
    {
        public string StaffId { get; set; } = string.Empty;
        public string StaffName { get; set; } = string.Empty;
        public int TotalScheduled { get; set; }
        public int CompletedOnTime { get; set; }
        public int MissedOrOverdue { get; set; }
        public int Cancelled { get; set; }
        public double ComplianceRate { get; set; }
    }

    // 6. Customer Acquisition Report
    public class CustomerAcquisitionReportItem
    {
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? AcquiredByStaff { get; set; }
        public int TotalOpportunitiesCount { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    // 7. Loss Analysis Report
    public class LossAnalysisReportItem
    {
        public string RecordType { get; set; } = string.Empty; // Lead or Opportunity
        public string CodeOrId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Company { get; set; }
        public decimal LostValue { get; set; }
        public string? ReasonOrRemarks { get; set; }
        public DateTime LostDate { get; set; }
        public string? AssignedStaff { get; set; }
    }

    // 8. Audit Summary Report
    public class AuditSummaryReportItem
    {
        public string StaffName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public int EventCount { get; set; }
        public DateTime LastEventTimestamp { get; set; }
    }
}

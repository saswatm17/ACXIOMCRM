using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Services
{
    public interface IReportService
    {
        Task<List<LeadConversionReportItem>> GetLeadConversionReportAsync(DateTime? fromDate, DateTime? toDate, string? staffId);
        Task<List<SalesPipelineReportItem>> GetSalesPipelineReportAsync(string? stage, string? staffId);
        Task<List<SalesPerformanceReportItem>> GetSalesPerformanceReportAsync(DateTime? fromDate, DateTime? toDate);
        Task<List<ActivitySummaryReportItem>> GetActivitySummaryReportAsync(DateTime? fromDate, DateTime? toDate, string? staffId);
        Task<List<FollowUpComplianceReportItem>> GetFollowUpComplianceReportAsync(DateTime? fromDate, DateTime? toDate, string? staffId);
        Task<List<CustomerAcquisitionReportItem>> GetCustomerAcquisitionReportAsync(DateTime? fromDate, DateTime? toDate, string? state);
        Task<List<LossAnalysisReportItem>> GetLossAnalysisReportAsync(DateTime? fromDate, DateTime? toDate);
        Task<List<AuditSummaryReportItem>> GetAuditSummaryReportAsync(DateTime? fromDate, DateTime? toDate, string? actionType);
        Task<IEnumerable<SelectListItem>> GetStaffDropdownAsync();
        string GenerateCsv<T>(IEnumerable<T> data, string[] headers, Func<T, object?[]> rowSelector);
    }
}

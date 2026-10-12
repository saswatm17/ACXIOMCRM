using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Models.Constants;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers
{
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public class ReportsController : Controller
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        public IActionResult Index()
        {
            var reports = new List<ReportIndexItem>
            {
                new ReportIndexItem
                {
                    ActionName = "LeadConversion",
                    Title = "Lead Conversion Report",
                    Description = "Analyze conversion velocity, conversion rates, and revenue generated from qualified leads.",
                    Category = "Marketing & Leads",
                    Icon = "bi-funnel-fill",
                    BadgeColor = "success"
                },
                new ReportIndexItem
                {
                    ActionName = "Pipeline",
                    Title = "Sales Pipeline Report",
                    Description = "Stage-by-stage opportunity distribution, weighted values, and anticipated closure dates.",
                    Category = "Sales Pipeline",
                    Icon = "bi-currency-dollar",
                    BadgeColor = "primary"
                },
                new ReportIndexItem
                {
                    ActionName = "SalesPerformance",
                    Title = "Sales Rep Performance",
                    Description = "Team quota attainment, total won deals, win rates, and individual revenue contribution.",
                    Category = "Team Management",
                    Icon = "bi-trophy-fill",
                    BadgeColor = "warning"
                },
                new ReportIndexItem
                {
                    ActionName = "ActivitySummary",
                    Title = "Activity Summary Report",
                    Description = "Volume of customer touchpoints across calls, meetings, emails, and completed tasks by representative.",
                    Category = "Productivity",
                    Icon = "bi-clock-history",
                    BadgeColor = "info"
                },
                new ReportIndexItem
                {
                    ActionName = "FollowUpCompliance",
                    Title = "Follow-Up Compliance Report",
                    Description = "On-time execution rates, missed callbacks, and follow-up discipline per sales representative.",
                    Category = "Quality & SLA",
                    Icon = "bi-calendar2-check-fill",
                    BadgeColor = "danger"
                },
                new ReportIndexItem
                {
                    ActionName = "CustomerAcquisition",
                    Title = "Customer Acquisition Report",
                    Description = "New client onboarding trends, geographic distribution, and lifetime revenue by customer.",
                    Category = "Accounts",
                    Icon = "bi-building-fill-check",
                    BadgeColor = "primary"
                },
                new ReportIndexItem
                {
                    ActionName = "LossAnalysis",
                    Title = "Loss Analysis Report",
                    Description = "Root causes, disqualified lead volumes, and lost deal amounts to improve future sales pitches.",
                    Category = "Sales Analysis",
                    Icon = "bi-graph-down-arrow",
                    BadgeColor = "secondary"
                },
                new ReportIndexItem
                {
                    ActionName = "AuditSummary",
                    Title = "Audit Trail Summary",
                    Description = "Aggregated security, mutation, and login operations across staff accounts and entity tables.",
                    Category = "Security & Governance",
                    Icon = "bi-shield-check",
                    BadgeColor = "dark"
                }
            };

            return View(reports);
        }

        // 1. Lead Conversion
        public async Task<IActionResult> LeadConversion(DateTime? fromDate, DateTime? toDate, string? staffId, bool export = false)
        {
            var data = await _reportService.GetLeadConversionReportAsync(fromDate, toDate, staffId);

            if (export)
            {
                var headers = new[] { "Lead Code", "Lead Name", "Company", "Source", "Status", "Created Date", "Converted", "Customer Name", "Deal Amount (INR)", "Assigned Staff" };
                var csv = _reportService.GenerateCsv(data, headers, item => new object?[]
                {
                    item.LeadCode, item.LeadName, item.CompanyName, item.Source, item.Status, item.CreatedDate.ToString("yyyy-MM-dd"),
                    item.IsConverted ? "Yes" : "No", item.CustomerName, item.DealAmount, item.AssignedToStaff
                });
                return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"LeadConversionReport_{DateTime.UtcNow:yyyyMMdd}.csv");
            }

            ViewBag.Filter = new ReportFilterViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                StaffId = staffId,
                StaffList = await _reportService.GetStaffDropdownAsync()
            };

            return View(data);
        }

        // 2. Sales Pipeline
        public async Task<IActionResult> Pipeline(string? stage, string? staffId, bool export = false)
        {
            var data = await _reportService.GetSalesPipelineReportAsync(stage, staffId);

            if (export)
            {
                var headers = new[] { "Deal ID", "Opportunity Name", "Customer", "Stage", "Probability (%)", "Amount (INR)", "Weighted Amount (INR)", "Expected Close Date", "Status", "Assigned Staff" };
                var csv = _reportService.GenerateCsv(data, headers, item => new object?[]
                {
                    item.OpportunityId, item.OpportunityName, item.CustomerName, item.Stage, item.Probability,
                    item.Amount, item.WeightedAmount, item.ExpectedCloseDate.ToString("yyyy-MM-dd"), item.Status, item.AssignedToStaff
                });
                return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"SalesPipelineReport_{DateTime.UtcNow:yyyyMMdd}.csv");
            }

            ViewBag.Filter = new ReportFilterViewModel
            {
                Stage = stage,
                StaffId = staffId,
                StaffList = await _reportService.GetStaffDropdownAsync()
            };

            return View(data);
        }

        // 3. Sales Performance
        public async Task<IActionResult> SalesPerformance(DateTime? fromDate, DateTime? toDate, bool export = false)
        {
            var data = await _reportService.GetSalesPerformanceReportAsync(fromDate, toDate);

            if (export)
            {
                var headers = new[] { "Staff Name", "Email", "Total Deals", "Won Deals Count", "Active Pipeline (INR)", "Won Revenue (INR)", "Win Rate (%)" };
                var csv = _reportService.GenerateCsv(data, headers, item => new object?[]
                {
                    item.StaffName, item.Email, item.TotalDeals, item.WonDealsCount, item.TotalPipelineValue, item.TotalWonRevenue, item.WinRate
                });
                return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"SalesPerformanceReport_{DateTime.UtcNow:yyyyMMdd}.csv");
            }

            ViewBag.Filter = new ReportFilterViewModel { FromDate = fromDate, ToDate = toDate };
            return View(data);
        }

        // 4. Activity Summary
        public async Task<IActionResult> ActivitySummary(DateTime? fromDate, DateTime? toDate, string? staffId, bool export = false)
        {
            var data = await _reportService.GetActivitySummaryReportAsync(fromDate, toDate, staffId);

            if (export)
            {
                var headers = new[] { "Staff Name", "Total Activities", "Calls", "Meetings", "Emails", "Tasks", "Completed", "Completion Rate (%)" };
                var csv = _reportService.GenerateCsv(data, headers, item => new object?[]
                {
                    item.StaffName, item.TotalActivities, item.CallsCount, item.MeetingsCount, item.EmailsCount, item.TasksCount, item.CompletedCount, item.CompletionRate
                });
                return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"ActivitySummaryReport_{DateTime.UtcNow:yyyyMMdd}.csv");
            }

            ViewBag.Filter = new ReportFilterViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                StaffId = staffId,
                StaffList = await _reportService.GetStaffDropdownAsync()
            };

            return View(data);
        }

        // 5. Follow-Up Compliance
        public async Task<IActionResult> FollowUpCompliance(DateTime? fromDate, DateTime? toDate, string? staffId, bool export = false)
        {
            var data = await _reportService.GetFollowUpComplianceReportAsync(fromDate, toDate, staffId);

            if (export)
            {
                var headers = new[] { "Staff Name", "Total Scheduled", "Completed", "Missed / Overdue", "Cancelled", "Compliance Rate (%)" };
                var csv = _reportService.GenerateCsv(data, headers, item => new object?[]
                {
                    item.StaffName, item.TotalScheduled, item.CompletedOnTime, item.MissedOrOverdue, item.Cancelled, item.ComplianceRate
                });
                return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"FollowUpComplianceReport_{DateTime.UtcNow:yyyyMMdd}.csv");
            }

            ViewBag.Filter = new ReportFilterViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                StaffId = staffId,
                StaffList = await _reportService.GetStaffDropdownAsync()
            };

            return View(data);
        }

        // 6. Customer Acquisition
        public async Task<IActionResult> CustomerAcquisition(DateTime? fromDate, DateTime? toDate, string? state, bool export = false)
        {
            var data = await _reportService.GetCustomerAcquisitionReportAsync(fromDate, toDate, state);

            if (export)
            {
                var headers = new[] { "Customer Code", "Customer Name", "Company", "City", "State", "Acquired Date", "Onboarded By", "Opportunities Count", "Won Revenue (INR)" };
                var csv = _reportService.GenerateCsv(data, headers, item => new object?[]
                {
                    item.CustomerCode, item.CustomerName, item.CompanyName, item.City, item.State, item.CreatedDate.ToString("yyyy-MM-dd"), item.AcquiredByStaff, item.TotalOpportunitiesCount, item.TotalRevenue
                });
                return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"CustomerAcquisitionReport_{DateTime.UtcNow:yyyyMMdd}.csv");
            }

            ViewBag.Filter = new ReportFilterViewModel { FromDate = fromDate, ToDate = toDate, State = state };
            return View(data);
        }

        // 7. Loss Analysis
        public async Task<IActionResult> LossAnalysis(DateTime? fromDate, DateTime? toDate, bool export = false)
        {
            var data = await _reportService.GetLossAnalysisReportAsync(fromDate, toDate);

            if (export)
            {
                var headers = new[] { "Type", "Code/ID", "Name", "Company", "Lost Value (INR)", "Reason / Remarks", "Lost Date", "Assigned Staff" };
                var csv = _reportService.GenerateCsv(data, headers, item => new object?[]
                {
                    item.RecordType, item.CodeOrId, item.Name, item.Company, item.LostValue, item.ReasonOrRemarks, item.LostDate.ToString("yyyy-MM-dd"), item.AssignedStaff
                });
                return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"LossAnalysisReport_{DateTime.UtcNow:yyyyMMdd}.csv");
            }

            ViewBag.Filter = new ReportFilterViewModel { FromDate = fromDate, ToDate = toDate };
            return View(data);
        }

        // 8. Audit Summary
        public async Task<IActionResult> AuditSummary(DateTime? fromDate, DateTime? toDate, string? actionType, bool export = false)
        {
            var data = await _reportService.GetAuditSummaryReportAsync(fromDate, toDate, actionType);

            if (export)
            {
                var headers = new[] { "Staff Member", "Action", "Entity Name", "Total Events Count", "Last Event (UTC)" };
                var csv = _reportService.GenerateCsv(data, headers, item => new object?[]
                {
                    item.StaffName, item.Action, item.EntityName, item.EventCount, item.LastEventTimestamp.ToString("yyyy-MM-dd HH:mm:ss")
                });
                return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"AuditSummaryReport_{DateTime.UtcNow:yyyyMMdd}.csv");
            }

            ViewBag.Filter = new ReportFilterViewModel { FromDate = fromDate, ToDate = toDate, Status = actionType };
            return View(data);
        }
    }
}

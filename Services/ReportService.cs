using System.Text;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Constants;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;

        public ReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<LeadConversionReportItem>> GetLeadConversionReportAsync(DateTime? fromDate, DateTime? toDate, string? staffId)
        {
            var query = _context.Leads
                .Include(l => l.Customer)
                .Include(l => l.Opportunities)
                .Include(l => l.AssignedToUser)
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(l => l.CreatedDate >= fromDate.Value.Date);

            if (toDate.HasValue)
                query = query.Where(l => l.CreatedDate < toDate.Value.Date.AddDays(1));

            if (!string.IsNullOrWhiteSpace(staffId))
                query = query.Where(l => l.AssignedTo == staffId);

            var leads = await query.OrderByDescending(l => l.CreatedDate).ToListAsync();

            return leads.Select(l => new LeadConversionReportItem
            {
                LeadCode = l.LeadCode,
                LeadName = l.LeadName,
                CompanyName = l.CompanyName,
                Source = l.Source,
                Status = l.Status,
                CreatedDate = l.CreatedDate,
                IsConverted = l.Status == LeadStatus.Converted,
                CustomerName = l.Customer?.CustomerName,
                DealAmount = l.Opportunities.FirstOrDefault()?.Amount ?? (l.Status == LeadStatus.Converted ? l.ExpectedValue : null),
                AssignedToStaff = l.AssignedToUser?.FullName ?? "Unassigned"
            }).ToList();
        }

        public async Task<List<SalesPipelineReportItem>> GetSalesPipelineReportAsync(string? stage, string? staffId)
        {
            var query = _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.AssignedToUser)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(stage))
                query = query.Where(o => o.Stage == stage);

            if (!string.IsNullOrWhiteSpace(staffId))
                query = query.Where(o => o.AssignedTo == staffId);

            var deals = await query.OrderByDescending(o => o.Amount).ToListAsync();

            return deals.Select(o => new SalesPipelineReportItem
            {
                OpportunityId = o.OpportunityId,
                OpportunityName = o.OpportunityName,
                CustomerName = o.Customer?.CustomerName ?? "Unknown Customer",
                Stage = o.Stage,
                Probability = o.Probability,
                Amount = o.Amount,
                WeightedAmount = (o.Amount * o.Probability) / 100m,
                ExpectedCloseDate = o.ExpectedCloseDate,
                Status = o.Status,
                AssignedToStaff = o.AssignedToUser?.FullName ?? "Unassigned"
            }).ToList();
        }

        public async Task<List<SalesPerformanceReportItem>> GetSalesPerformanceReportAsync(DateTime? fromDate, DateTime? toDate)
        {
            var users = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            var oppQuery = _context.Opportunities.AsQueryable();

            if (fromDate.HasValue)
                oppQuery = oppQuery.Where(o => o.CreatedDate >= fromDate.Value.Date);

            if (toDate.HasValue)
                oppQuery = oppQuery.Where(o => o.CreatedDate < toDate.Value.Date.AddDays(1));

            var allOpps = await oppQuery.ToListAsync();
            var report = new List<SalesPerformanceReportItem>();

            foreach (var u in users)
            {
                var userDeals = allOpps.Where(o => o.AssignedTo == u.Id).ToList();
                var wonDeals = userDeals.Where(o => o.Status == OpportunityStatus.Won || o.Stage == OpportunityStage.Won).ToList();

                int totalDeals = userDeals.Count;
                int wonCount = wonDeals.Count;
                decimal pipelineVal = userDeals.Where(o => o.Status == OpportunityStatus.Open).Sum(o => o.Amount);
                decimal wonRevenue = wonDeals.Sum(o => o.Amount);
                double winRate = totalDeals > 0 ? Math.Round((double)wonCount / totalDeals * 100, 1) : 0;

                report.Add(new SalesPerformanceReportItem
                {
                    StaffId = u.Id,
                    StaffName = u.FullName,
                    Email = u.Email ?? "",
                    TotalDeals = totalDeals,
                    WonDealsCount = wonCount,
                    TotalPipelineValue = pipelineVal,
                    TotalWonRevenue = wonRevenue,
                    WinRate = winRate
                });
            }

            return report.OrderByDescending(r => r.TotalWonRevenue).ToList();
        }

        public async Task<List<ActivitySummaryReportItem>> GetActivitySummaryReportAsync(DateTime? fromDate, DateTime? toDate, string? staffId)
        {
            var query = _context.Activities.Include(a => a.AssignedToUser).AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(a => a.ActivityDate >= fromDate.Value.Date);

            if (toDate.HasValue)
                query = query.Where(a => a.ActivityDate < toDate.Value.Date.AddDays(1));

            if (!string.IsNullOrWhiteSpace(staffId))
                query = query.Where(a => a.AssignedTo == staffId);

            var activities = await query.ToListAsync();
            var grouped = activities.GroupBy(a => a.AssignedToUser?.FullName ?? "Unassigned");

            var result = new List<ActivitySummaryReportItem>();

            foreach (var g in grouped)
            {
                var total = g.Count();
                var completed = g.Count(a => a.Status == ActivityStatus.Completed);
                result.Add(new ActivitySummaryReportItem
                {
                    StaffName = g.Key,
                    TotalActivities = total,
                    CallsCount = g.Count(a => a.ActivityType == ActivityType.Call),
                    MeetingsCount = g.Count(a => a.ActivityType == ActivityType.Meeting),
                    EmailsCount = g.Count(a => a.ActivityType == ActivityType.Email),
                    TasksCount = g.Count(a => a.ActivityType == ActivityType.Task),
                    CompletedCount = completed,
                    CompletionRate = total > 0 ? Math.Round((double)completed / total * 100, 1) : 0
                });
            }

            return result.OrderByDescending(r => r.TotalActivities).ToList();
        }

        public async Task<List<FollowUpComplianceReportItem>> GetFollowUpComplianceReportAsync(DateTime? fromDate, DateTime? toDate, string? staffId)
        {
            var query = _context.FollowUps.Include(f => f.AssignedToUser).AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(f => f.FollowUpDate >= fromDate.Value.Date);

            if (toDate.HasValue)
                query = query.Where(f => f.FollowUpDate < toDate.Value.Date.AddDays(1));

            if (!string.IsNullOrWhiteSpace(staffId))
                query = query.Where(f => f.AssignedTo == staffId);

            var followUps = await query.ToListAsync();
            var grouped = followUps.GroupBy(f => f.AssignedToUser?.FullName ?? "Unassigned");

            var result = new List<FollowUpComplianceReportItem>();

            foreach (var g in grouped)
            {
                var total = g.Count();
                var completed = g.Count(f => f.Status == FollowUpStatus.Completed);
                var missed = g.Count(f => f.Status == FollowUpStatus.Missed || (f.Status == FollowUpStatus.Planned && f.FollowUpDate < DateTime.UtcNow));
                var cancelled = g.Count(f => f.Status == FollowUpStatus.Cancelled);

                result.Add(new FollowUpComplianceReportItem
                {
                    StaffName = g.Key,
                    TotalScheduled = total,
                    CompletedOnTime = completed,
                    MissedOrOverdue = missed,
                    Cancelled = cancelled,
                    ComplianceRate = total > 0 ? Math.Round((double)completed / total * 100, 1) : 0
                });
            }

            return result.OrderByDescending(r => r.ComplianceRate).ToList();
        }

        public async Task<List<CustomerAcquisitionReportItem>> GetCustomerAcquisitionReportAsync(DateTime? fromDate, DateTime? toDate, string? state)
        {
            var query = _context.Customers
                .Include(c => c.CreatedByUser)
                .Include(c => c.Opportunities)
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(c => c.CreatedDate >= fromDate.Value.Date);

            if (toDate.HasValue)
                query = query.Where(c => c.CreatedDate < toDate.Value.Date.AddDays(1));

            if (!string.IsNullOrWhiteSpace(state))
                query = query.Where(c => c.State == state);

            var customers = await query.OrderByDescending(c => c.CreatedDate).ToListAsync();

            return customers.Select(c => new CustomerAcquisitionReportItem
            {
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                CompanyName = c.CompanyName,
                City = c.City,
                State = c.State,
                CreatedDate = c.CreatedDate,
                AcquiredByStaff = c.CreatedByUser?.FullName ?? "Direct Onboarding",
                TotalOpportunitiesCount = c.Opportunities.Count,
                TotalRevenue = c.Opportunities
                    .Where(o => o.Status == OpportunityStatus.Won || o.Stage == OpportunityStage.Won)
                    .Sum(o => o.Amount)
            }).ToList();
        }

        public async Task<List<LossAnalysisReportItem>> GetLossAnalysisReportAsync(DateTime? fromDate, DateTime? toDate)
        {
            var report = new List<LossAnalysisReportItem>();

            // 1. Lost or Unqualified Leads
            var leadQuery = _context.Leads
                .Include(l => l.AssignedToUser)
                .Where(l => l.Status == LeadStatus.Lost || l.Status == LeadStatus.Unqualified);

            if (fromDate.HasValue)
                leadQuery = leadQuery.Where(l => l.CreatedDate >= fromDate.Value.Date);
            if (toDate.HasValue)
                leadQuery = leadQuery.Where(l => l.CreatedDate < toDate.Value.Date.AddDays(1));

            var lostLeads = await leadQuery.ToListAsync();
            foreach (var l in lostLeads)
            {
                report.Add(new LossAnalysisReportItem
                {
                    RecordType = "Lead",
                    CodeOrId = l.LeadCode,
                    Name = l.LeadName,
                    Company = l.CompanyName,
                    LostValue = l.ExpectedValue,
                    ReasonOrRemarks = $"Status: {l.Status} (Source: {l.Source})",
                    LostDate = l.CreatedDate,
                    AssignedStaff = l.AssignedToUser?.FullName ?? "Unassigned"
                });
            }

            // 2. Closed Lost Opportunities
            var oppQuery = _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.AssignedToUser)
                .Where(o => o.Status == OpportunityStatus.Lost || o.Stage == OpportunityStage.Lost);

            if (fromDate.HasValue)
                oppQuery = oppQuery.Where(o => o.ExpectedCloseDate >= fromDate.Value.Date);
            if (toDate.HasValue)
                oppQuery = oppQuery.Where(o => o.ExpectedCloseDate < toDate.Value.Date.AddDays(1));

            var lostOpps = await oppQuery.ToListAsync();
            foreach (var o in lostOpps)
            {
                report.Add(new LossAnalysisReportItem
                {
                    RecordType = "Opportunity",
                    CodeOrId = $"OPP-{o.OpportunityId}",
                    Name = o.OpportunityName,
                    Company = o.Customer?.CustomerName,
                    LostValue = o.Amount,
                    ReasonOrRemarks = "Opportunity marked Closed Lost during pipeline evaluation",
                    LostDate = o.ExpectedCloseDate,
                    AssignedStaff = o.AssignedToUser?.FullName ?? "Unassigned"
                });
            }

            return report.OrderByDescending(r => r.LostValue).ToList();
        }

        public async Task<List<AuditSummaryReportItem>> GetAuditSummaryReportAsync(DateTime? fromDate, DateTime? toDate, string? actionType)
        {
            var query = _context.AuditLogs.Include(a => a.User).AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(a => a.CreatedDate >= fromDate.Value.Date);

            if (toDate.HasValue)
                query = query.Where(a => a.CreatedDate < toDate.Value.Date.AddDays(1));

            if (!string.IsNullOrWhiteSpace(actionType))
                query = query.Where(a => a.Action == actionType);

            var logs = await query.ToListAsync();

            return logs
                .GroupBy(l => new { User = l.User?.FullName ?? "System", l.Action, l.EntityName })
                .Select(g => new AuditSummaryReportItem
                {
                    StaffName = g.Key.User,
                    Action = g.Key.Action,
                    EntityName = g.Key.EntityName,
                    EventCount = g.Count(),
                    LastEventTimestamp = g.Max(l => l.CreatedDate)
                })
                .OrderByDescending(r => r.EventCount)
                .ToList();
        }

        public async Task<IEnumerable<SelectListItem>> GetStaffDropdownAsync()
        {
            return await _context.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.FullName)
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = $"{u.FullName} ({u.Email})"
                })
                .ToListAsync();
        }

        public string GenerateCsv<T>(IEnumerable<T> data, string[] headers, Func<T, object?[]> rowSelector)
        {
            var sb = new StringBuilder();

            // Header line
            sb.AppendLine(string.Join(",", headers.Select(EscapeCsvValue)));

            // Rows
            foreach (var item in data)
            {
                var values = rowSelector(item);
                sb.AppendLine(string.Join(",", values.Select(v => EscapeCsvValue(v?.ToString() ?? ""))));
            }

            return sb.ToString();
        }

        private static string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return "\"\"";

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\r") || value.Contains("\n"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }

            return value;
        }
    }
}

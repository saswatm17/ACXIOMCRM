using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Models.Constants;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserScopeService _userScopeService;

        public DashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IUserScopeService userScopeService)
        {
            _context = context;
            _userManager = userManager;
            _userScopeService = userScopeService;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var role = await _userScopeService.GetCurrentUserRoleAsync();
            var currentUserId = user?.Id ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            bool isElevated = _userScopeService.IsAdmin() || _userScopeService.IsManager();

            // 1. Base scoped queries
            var customerQuery = _context.Customers.AsQueryable();
            var leadQuery = _context.Leads.AsQueryable();
            var oppQuery = _context.Opportunities.AsQueryable();
            var followUpQuery = _context.FollowUps.AsQueryable();
            var activityQuery = _context.Activities.AsQueryable();

            if (!isElevated)
            {
                leadQuery = leadQuery.Where(l => l.AssignedTo == currentUserId);
                oppQuery = oppQuery.Where(o => o.AssignedTo == currentUserId);
                followUpQuery = followUpQuery.Where(f => f.AssignedTo == currentUserId);
                activityQuery = activityQuery.Where(a => a.AssignedTo == currentUserId);
            }

            var now = DateTime.UtcNow;
            var today = now.Date;

            // 2. High-Level KPI Aggregations
            int totalCustomers = await customerQuery.CountAsync(c => c.Status == CustomerStatus.Active);
            int totalLeads = await leadQuery.CountAsync();
            int convertedLeads = await leadQuery.CountAsync(l => l.Status == LeadStatus.Converted);
            double conversionRate = totalLeads > 0 ? Math.Round((double)convertedLeads / totalLeads * 100, 1) : 0;

            var activeDealsQuery = oppQuery.Where(o => o.Status == OpportunityStatus.Open);
            decimal totalPipelineValue = await activeDealsQuery.SumAsync(o => (decimal?)o.Amount) ?? 0m;
            decimal weightedPipelineValue = await activeDealsQuery.SumAsync(o => (decimal?)(o.Amount * o.Probability) / 100m) ?? 0m;
            decimal closedWonValue = await oppQuery
                .Where(o => o.Status == OpportunityStatus.Won || o.Stage == OpportunityStage.Won)
                .SumAsync(o => (decimal?)o.Amount) ?? 0m;

            int followUpsDueToday = await followUpQuery
                .CountAsync(f => f.FollowUpDate.Date == today && f.Status == FollowUpStatus.Planned);
            int followUpsOverdue = await followUpQuery
                .CountAsync(f => f.Status == FollowUpStatus.Missed || (f.FollowUpDate < now && f.Status == FollowUpStatus.Planned));

            // 3. Chart 1: Lead Status Breakdown
            var leadStatuses = new[] { LeadStatus.New, LeadStatus.Contacted, LeadStatus.Qualified, LeadStatus.Converted, LeadStatus.Lost };
            var leadStatusCountsList = new List<int>();
            foreach (var status in leadStatuses)
            {
                int count = await leadQuery.CountAsync(l => l.Status == status);
                leadStatusCountsList.Add(count);
            }

            // 4. Chart 2: Pipeline by Stage
            var stages = OpportunityStage.All;
            var stageValuesList = new List<decimal>();
            var stageCountsList = new List<int>();
            foreach (var stage in stages)
            {
                var stageDeals = await oppQuery.Where(o => o.Stage == stage).ToListAsync();
                stageValuesList.Add(stageDeals.Sum(o => o.Amount));
                stageCountsList.Add(stageDeals.Count);
            }

            // 5. Chart 3: Monthly Revenue Trend (Trailing 6 Months)
            var monthlyLabels = new List<string>();
            var monthlyRevenue = new List<decimal>();

            var wonDeals = await oppQuery
                .Where(o => o.Status == OpportunityStatus.Won || o.Stage == OpportunityStage.Won)
                .ToListAsync();

            for (int i = 5; i >= 0; i--)
            {
                var targetMonth = now.AddMonths(-i);
                var monthName = targetMonth.ToString("MMM yyyy", CultureInfo.InvariantCulture);
                monthlyLabels.Add(monthName);

                var revenue = wonDeals
                    .Where(o => o.ExpectedCloseDate.Year == targetMonth.Year && o.ExpectedCloseDate.Month == targetMonth.Month)
                    .Sum(o => o.Amount);

                monthlyRevenue.Add(revenue);
            }

            // 6. Actionable Dashboard Widgets
            var urgentFollowUps = await followUpQuery
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Where(f => f.Status == FollowUpStatus.Planned || f.Status == FollowUpStatus.Missed)
                .OrderBy(f => f.FollowUpDate)
                .Take(5)
                .ToListAsync();

            var topActiveDeals = await activeDealsQuery
                .Include(o => o.Customer)
                .Include(o => o.AssignedToUser)
                .OrderByDescending(o => o.Amount)
                .Take(5)
                .ToListAsync();

            var recentActivities = await activityQuery
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .Include(a => a.AssignedToUser)
                .OrderByDescending(a => a.ActivityDate)
                .Take(5)
                .ToListAsync();

            var model = new DashboardViewModel
            {
                FullName = user?.FullName ?? User.Identity?.Name ?? "Executive",
                Email = user?.Email ?? "",
                Role = role,
                TotalCustomers = totalCustomers,
                TotalLeads = totalLeads,
                ConvertedLeadsCount = convertedLeads,
                LeadConversionRate = conversionRate,
                TotalPipelineValue = totalPipelineValue,
                WeightedPipelineValue = weightedPipelineValue,
                ClosedWonValue = closedWonValue,
                FollowUpsDueTodayCount = followUpsDueToday,
                FollowUpsOverdueCount = followUpsOverdue,
                LeadStatusLabels = leadStatuses,
                LeadStatusCounts = leadStatusCountsList.ToArray(),
                StageLabels = stages,
                StageValues = stageValuesList.ToArray(),
                StageDealsCount = stageCountsList.ToArray(),
                MonthlyRevenueLabels = monthlyLabels.ToArray(),
                MonthlyRevenueValues = monthlyRevenue.ToArray(),
                UrgentFollowUps = urgentFollowUps,
                TopActiveDeals = topActiveDeals,
                RecentActivities = recentActivities
            };

            return View(model);
        }
    }
}

using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels
{
    public class DashboardViewModel
    {
        // User & Role Context
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        // KPI Metrics
        public int TotalCustomers { get; set; }
        public int TotalLeads { get; set; }
        public int ConvertedLeadsCount { get; set; }
        public double LeadConversionRate { get; set; }
        public decimal TotalPipelineValue { get; set; }
        public decimal WeightedPipelineValue { get; set; }
        public decimal ClosedWonValue { get; set; }
        public int FollowUpsDueTodayCount { get; set; }
        public int FollowUpsOverdueCount { get; set; }

        // Chart 1: Lead Status Breakdown
        public string[] LeadStatusLabels { get; set; } = Array.Empty<string>();
        public int[] LeadStatusCounts { get; set; } = Array.Empty<int>();

        // Chart 2: Pipeline by Stage
        public string[] StageLabels { get; set; } = Array.Empty<string>();
        public decimal[] StageValues { get; set; } = Array.Empty<decimal>();
        public int[] StageDealsCount { get; set; } = Array.Empty<int>();

        // Chart 3: Monthly Sales / Won Revenue Trend
        public string[] MonthlyRevenueLabels { get; set; } = Array.Empty<string>();
        public decimal[] MonthlyRevenueValues { get; set; } = Array.Empty<decimal>();

        // Widgets
        public List<FollowUp> UrgentFollowUps { get; set; } = new List<FollowUp>();
        public List<Opportunity> TopActiveDeals { get; set; } = new List<Opportunity>();
        public List<Activity> RecentActivities { get; set; } = new List<Activity>();
    }
}

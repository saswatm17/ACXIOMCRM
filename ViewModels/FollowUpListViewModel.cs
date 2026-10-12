using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels
{
    public class FollowUpListViewModel
    {
        public IEnumerable<FollowUp> FollowUps { get; set; } = new List<FollowUp>();

        public string Filter { get; set; } = "All"; // All, Today, Upcoming, Overdue, Completed, Missed, Cancelled
        public string? TypeFilter { get; set; }
        public string? AssignedToFilter { get; set; }
        public string? SearchTerm { get; set; }

        public int TotalCount { get; set; }
        public int TodayCount { get; set; }
        public int UpcomingCount { get; set; }
        public int OverdueCount { get; set; }
        public int CompletedCount { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; } = 1;

        public IEnumerable<SelectListItem> UsersList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> TypesList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> StatusesList { get; set; } = new List<SelectListItem>();
    }
}

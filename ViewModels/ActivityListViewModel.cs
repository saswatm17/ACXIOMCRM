using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels
{
    public class ActivityListViewModel
    {
        public IEnumerable<Activity> Activities { get; set; } = new List<Activity>();

        public string? TypeFilter { get; set; }
        public string? StatusFilter { get; set; }
        public string? AssignedToFilter { get; set; }
        public string? SearchTerm { get; set; }
        public string ViewMode { get; set; } = "table"; // "table" or "timeline"

        public int TotalActivitiesCount { get; set; }
        public int PendingTasksCount { get; set; }
        public int CallsCount { get; set; }
        public int MeetingsCount { get; set; }
        public int CompletedActivitiesCount { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; } = 1;

        public IEnumerable<SelectListItem> UsersList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> TypesList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> StatusesList { get; set; } = new List<SelectListItem>();
    }
}

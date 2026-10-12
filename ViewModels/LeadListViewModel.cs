using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels
{
    public class LeadListViewModel
    {
        public IEnumerable<Lead> Leads { get; set; } = new List<Lead>();
        public string? SearchTerm { get; set; }
        public string? SelectedStatus { get; set; }
        public string? SelectedSource { get; set; }
        public string? SelectedAssignedTo { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 10;

        public IEnumerable<SelectListItem> UsersList { get; set; } = new List<SelectListItem>();

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}

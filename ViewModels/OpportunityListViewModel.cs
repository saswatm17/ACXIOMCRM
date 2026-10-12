using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels
{
    public class OpportunityListViewModel
    {
        public IEnumerable<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
        public string? SearchTerm { get; set; }
        public string? SelectedStage { get; set; }
        public string? SelectedStatus { get; set; }
        public string? SelectedAssignedTo { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 10;

        // KPI metrics
        public decimal TotalPipelineValue { get; set; }
        public decimal TotalWeightedValue { get; set; }
        public decimal WonValue { get; set; }
        public int OpenDealsCount { get; set; }
        public int WonDealsCount { get; set; }

        public IEnumerable<SelectListItem> UsersList { get; set; } = new List<SelectListItem>();

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class StagePipelineSummary
    {
        public string StageName { get; set; } = string.Empty;
        public int DealsCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal WeightedAmount { get; set; }
        public List<Opportunity> Deals { get; set; } = new List<Opportunity>();
    }

    public class OpportunityPipelineViewModel
    {
        public List<StagePipelineSummary> Stages { get; set; } = new List<StagePipelineSummary>();
        public decimal TotalPipelineValue { get; set; }
        public decimal TotalWeightedValue { get; set; }
        public int TotalDealsCount { get; set; }
    }
}

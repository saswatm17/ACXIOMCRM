using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Services
{
    public interface IOpportunityService
    {
        Task<OpportunityListViewModel> GetOpportunitiesAsync(
            string? search,
            string? stage,
            string? status,
            string? assignedTo,
            int page,
            int pageSize,
            string? currentUserId,
            bool isElevatedRole);

        Task<OpportunityPipelineViewModel> GetPipelineSummaryAsync(string? currentUserId, bool isElevatedRole);
        Task<Opportunity?> GetOpportunityByIdAsync(int id);
        Task<(bool Success, string? ErrorMessage, Opportunity? Opportunity)> CreateOpportunityAsync(OpportunityFormViewModel model, string currentUserId, string? ipAddress);
        Task<(bool Success, string? ErrorMessage)> UpdateOpportunityAsync(OpportunityFormViewModel model, string currentUserId, string? ipAddress);
        Task<(bool Success, string? ErrorMessage)> ChangeStageAsync(int id, string newStage, string currentUserId, string? ipAddress);
        Task<IEnumerable<SelectListItem>> GetCustomersDropdownAsync(string? currentUserId, bool isElevatedRole);
        Task<IEnumerable<SelectListItem>> GetLeadsDropdownAsync(string? currentUserId, bool isElevatedRole);
        Task<IEnumerable<SelectListItem>> GetUsersDropdownAsync();
    }
}

using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Services
{
    public interface ILeadService
    {
        Task<LeadListViewModel> GetLeadsAsync(
            string? search,
            string? status,
            string? source,
            string? assignedTo,
            int page,
            int pageSize,
            string? currentUserId,
            bool isElevatedRole);

        Task<Lead?> GetLeadByIdAsync(int id);
        Task<(bool Success, string? ErrorMessage, Lead? Lead)> CreateLeadAsync(LeadFormViewModel model, string currentUserId, string? ipAddress);
        Task<(bool Success, string? ErrorMessage)> UpdateLeadAsync(LeadFormViewModel model, string currentUserId, string? ipAddress);
        Task<(bool Success, string? ErrorMessage, Customer? Customer, Opportunity? Opportunity)> ConvertLeadAsync(LeadConvertViewModel model, string currentUserId, string? ipAddress);
        bool IsValidStatusTransition(string currentStatus, string newStatus);
        Task<IEnumerable<SelectListItem>> GetAssignableUsersAsync();
    }
}

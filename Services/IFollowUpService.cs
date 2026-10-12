using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Services
{
    public interface IFollowUpService
    {
        Task<FollowUpListViewModel> GetFollowUpsAsync(string currentUserId, string role, string? filter, string? type, string? assignedTo, string? search, int page = 1, int pageSize = 10);
        Task<FollowUp?> GetFollowUpByIdAsync(int id, string currentUserId, string role);
        Task<FollowUpFormViewModel> GetCreateViewModelAsync(int? customerId = null, int? leadId = null, string? currentUserId = null);
        Task<FollowUpFormViewModel?> GetEditViewModelAsync(int id, string currentUserId, string role);
        Task<FollowUp> CreateFollowUpAsync(FollowUpFormViewModel model, string currentUserId, string ipAddress);
        Task<bool> UpdateFollowUpAsync(FollowUpFormViewModel model, string currentUserId, string ipAddress);
        Task<bool> CompleteFollowUpAsync(int id, string? remarks, string currentUserId, string ipAddress);
        Task<bool> RescheduleFollowUpAsync(int id, DateTime newDate, string? remarks, string currentUserId, string ipAddress);
        Task<bool> CancelFollowUpAsync(int id, string? remarks, string currentUserId, string ipAddress);
        Task AutoMarkOverdueAsync();
    }
}

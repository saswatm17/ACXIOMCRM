using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Services
{
    public interface IActivityService
    {
        Task<ActivityListViewModel> GetActivitiesAsync(string currentUserId, string role, string? type, string? status, string? assignedTo, string? search, string viewMode = "table", int page = 1, int pageSize = 10);
        Task<List<Activity>> GetTimelineAsync(string currentUserId, string role, int? customerId = null, int? leadId = null, int take = 50);
        Task<Activity?> GetActivityByIdAsync(int id, string currentUserId, string role);
        Task<ActivityFormViewModel> GetCreateViewModelAsync(int? customerId = null, int? leadId = null, string? currentUserId = null);
        Task<ActivityFormViewModel?> GetEditViewModelAsync(int id, string currentUserId, string role);
        Task<Activity> CreateActivityAsync(ActivityFormViewModel model, string currentUserId, string ipAddress);
        Task<bool> UpdateActivityAsync(ActivityFormViewModel model, string currentUserId, string ipAddress);
        Task<bool> CompleteActivityAsync(int id, string? completionNote, string currentUserId, string ipAddress);
    }
}

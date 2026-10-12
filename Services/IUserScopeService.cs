using System.Security.Claims;
using AcxiomCRM.Models;

namespace AcxiomCRM.Services
{
    public interface IUserScopeService
    {
        string? GetCurrentUserId();
        Task<ApplicationUser?> GetCurrentUserAsync();
        Task<string> GetCurrentUserRoleAsync();
        bool IsAdmin();
        bool IsManager();
        bool IsSalesExecutive();
        Task<bool> CanAccessRecordAsync(string? recordOwnerId);
    }
}

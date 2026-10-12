using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using AcxiomCRM.Models;
using AcxiomCRM.Models.Constants;

namespace AcxiomCRM.Services
{
    public class UserScopeService : IUserScopeService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserScopeService(
            IHttpContextAccessor httpContextAccessor,
            UserManager<ApplicationUser> userManager)
        {
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
        }

        private ClaimsPrincipal? CurrentPrincipal => _httpContextAccessor.HttpContext?.User;

        public string? GetCurrentUserId()
        {
            return CurrentPrincipal?.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        public async Task<ApplicationUser?> GetCurrentUserAsync()
        {
            if (CurrentPrincipal == null)
            {
                return null;
            }
            return await _userManager.GetUserAsync(CurrentPrincipal);
        }

        public async Task<string> GetCurrentUserRoleAsync()
        {
            if (CurrentPrincipal == null)
            {
                return string.Empty;
            }

            if (CurrentPrincipal.IsInRole(AppRoles.Admin))
            {
                return AppRoles.Admin;
            }
            if (CurrentPrincipal.IsInRole(AppRoles.Manager))
            {
                return AppRoles.Manager;
            }
            if (CurrentPrincipal.IsInRole(AppRoles.SalesExecutive))
            {
                return AppRoles.SalesExecutive;
            }

            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);
                return roles.FirstOrDefault() ?? string.Empty;
            }

            return string.Empty;
        }

        public bool IsAdmin()
        {
            return CurrentPrincipal?.IsInRole(AppRoles.Admin) ?? false;
        }

        public bool IsManager()
        {
            return CurrentPrincipal?.IsInRole(AppRoles.Manager) ?? false;
        }

        public bool IsSalesExecutive()
        {
            return CurrentPrincipal?.IsInRole(AppRoles.SalesExecutive) ?? false;
        }

        public Task<bool> CanAccessRecordAsync(string? recordOwnerId)
        {
            // Admin and Manager roles have visibility across the organization
            if (IsAdmin() || IsManager())
            {
                return Task.FromResult(true);
            }

            // Sales Executives can only access records assigned to themselves
            var currentUserId = GetCurrentUserId();
            var canAccess = !string.IsNullOrEmpty(currentUserId) && currentUserId == recordOwnerId;
            return Task.FromResult(canAccess);
        }
    }
}

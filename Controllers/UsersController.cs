using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Models;
using AcxiomCRM.Models.Constants;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers
{
    [Authorize(Roles = AppRoles.Admin)]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IAuditService _auditService;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IAuditService auditService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _auditService = auditService;
        }

        public async Task<IActionResult> Index(string? role, string? status, string? search)
        {
            var users = await _userManager.Users.OrderBy(u => u.FullName).ToListAsync();
            var listItems = new List<UserListItemViewModel>();

            foreach (var u in users)
            {
                var userRoles = await _userManager.GetRolesAsync(u);
                var isLockedOut = await _userManager.IsLockedOutAsync(u);

                listItems.Add(new UserListItemViewModel
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email ?? string.Empty,
                    PhoneNumber = u.PhoneNumber,
                    Role = userRoles.FirstOrDefault() ?? "None",
                    IsActive = u.IsActive,
                    IsLockedOut = isLockedOut,
                    LockoutEnd = u.LockoutEnd,
                    CreatedDate = u.CreatedDate
                });
            }

            int totalUsers = listItems.Count;
            int activeUsers = listItems.Count(u => u.IsActive);
            int inactiveUsers = listItems.Count(u => !u.IsActive);
            int lockedOutUsers = listItems.Count(u => u.IsLockedOut);

            var filtered = listItems.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(role))
            {
                filtered = filtered.Where(u => u.Role.Equals(role, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                switch (status.ToLower())
                {
                    case "active":
                        filtered = filtered.Where(u => u.IsActive && !u.IsLockedOut);
                        break;
                    case "inactive":
                        filtered = filtered.Where(u => !u.IsActive);
                        break;
                    case "locked":
                        filtered = filtered.Where(u => u.IsLockedOut);
                        break;
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                filtered = filtered.Where(u =>
                    u.FullName.ToLower().Contains(term) ||
                    u.Email.ToLower().Contains(term) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(term)));
            }

            var roles = await _roleManager.Roles.Select(r => new SelectListItem
            {
                Value = r.Name,
                Text = r.Name
            }).ToListAsync();

            var viewModel = new UserListViewModel
            {
                Users = filtered.ToList(),
                RoleFilter = role,
                StatusFilter = status,
                SearchTerm = search,
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                InactiveUsers = inactiveUsers,
                LockedOutUsers = lockedOutUsers,
                RolesList = roles
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Create()
        {
            var model = new CreateUserViewModel
            {
                RolesList = await GetRolesSelectListAsync()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            if (!ModelState.IsValid)
            {
                model.RolesList = await GetRolesSelectListAsync();
                return View(model);
            }

            var existing = await _userManager.FindByEmailAsync(model.Email);
            if (existing != null)
            {
                ModelState.AddModelError(nameof(model.Email), "A user with this email address already exists.");
                model.RolesList = await GetRolesSelectListAsync();
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email.Trim().ToLower(),
                Email = model.Email.Trim().ToLower(),
                FullName = model.FullName.Trim(),
                PhoneNumber = model.PhoneNumber?.Trim(),
                IsActive = model.IsActive,
                CreatedDate = DateTime.UtcNow,
                EmailConfirmed = true,
                LockoutEnabled = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, err.Description);
                }
                model.RolesList = await GetRolesSelectListAsync();
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, model.Role);

            await _auditService.LogAsync(
                currentUserId,
                AuditAction.Create,
                nameof(ApplicationUser),
                user.Id,
                null,
                new { user.Email, user.FullName, Role = model.Role, user.IsActive },
                ipAddress);

            TempData["SuccessMessage"] = $"Staff account for '{user.FullName}' ({model.Role}) successfully created.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);

            var model = new EditUserViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Role = roles.FirstOrDefault() ?? AppRoles.SalesExecutive,
                IsActive = user.IsActive,
                RolesList = await GetRolesSelectListAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            if (!ModelState.IsValid)
            {
                model.RolesList = await GetRolesSelectListAsync();
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return NotFound();
            }

            // Security guard: Ensure at least one active Admin exists
            var currentRoles = await _userManager.GetRolesAsync(user);
            bool wasAdmin = currentRoles.Contains(AppRoles.Admin);
            bool demotingAdmin = wasAdmin && model.Role != AppRoles.Admin;
            bool deactivatingAdmin = wasAdmin && !model.IsActive;

            if (demotingAdmin || deactivatingAdmin)
            {
                var allAdmins = await _userManager.GetUsersInRoleAsync(AppRoles.Admin);
                int activeAdminsCount = allAdmins.Count(a => a.IsActive && a.Id != user.Id);

                if (activeAdminsCount == 0)
                {
                    ModelState.AddModelError(string.Empty, "Cannot demote or deactivate the only active Administrator in the system.");
                    model.RolesList = await GetRolesSelectListAsync();
                    return View(model);
                }
            }

            var oldSnapshot = new
            {
                user.FullName,
                user.PhoneNumber,
                user.IsActive,
                Role = currentRoles.FirstOrDefault()
            };

            user.FullName = model.FullName.Trim();
            user.PhoneNumber = model.PhoneNumber?.Trim();
            user.IsActive = model.IsActive;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var err in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, err.Description);
                }
                model.RolesList = await GetRolesSelectListAsync();
                return View(model);
            }

            // Update Role if changed
            if (!currentRoles.Contains(model.Role))
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, model.Role);

                await _auditService.LogAsync(
                    currentUserId,
                    AuditAction.RoleChange,
                    nameof(ApplicationUser),
                    user.Id,
                    new { PreviousRole = currentRoles.FirstOrDefault() },
                    new { NewRole = model.Role },
                    ipAddress);
            }

            await _auditService.LogAsync(
                currentUserId,
                AuditAction.Update,
                nameof(ApplicationUser),
                user.Id,
                oldSnapshot,
                new { user.FullName, user.PhoneNumber, user.IsActive, Role = model.Role },
                ipAddress);

            TempData["SuccessMessage"] = $"User profile for '{user.FullName}' successfully updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (user.Id == currentUserId && user.IsActive)
            {
                TempData["ErrorMessage"] = "You cannot deactivate your own administrative account.";
                return RedirectToAction(nameof(Index));
            }

            bool newStatus = !user.IsActive;

            // Check if deactivating last admin
            if (!newStatus && await _userManager.IsInRoleAsync(user, AppRoles.Admin))
            {
                var allAdmins = await _userManager.GetUsersInRoleAsync(AppRoles.Admin);
                if (allAdmins.Count(a => a.IsActive && a.Id != user.Id) == 0)
                {
                    TempData["ErrorMessage"] = "Cannot deactivate the only active Administrator.";
                    return RedirectToAction(nameof(Index));
                }
            }

            user.IsActive = newStatus;
            await _userManager.UpdateAsync(user);

            // If deactivated, reset security stamp so active cookies are invalidated
            if (!user.IsActive)
            {
                await _userManager.UpdateSecurityStampAsync(user);
            }

            await _auditService.LogAsync(
                currentUserId,
                AuditAction.Update,
                nameof(ApplicationUser),
                user.Id,
                new { IsActive = !newStatus },
                new { IsActive = newStatus },
                ipAddress);

            TempData["SuccessMessage"] = $"Account for '{user.FullName}' is now {(user.IsActive ? "Active" : "Inactive")}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(string id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);

            await _auditService.LogAsync(
                currentUserId,
                AuditAction.Update,
                nameof(ApplicationUser),
                user.Id,
                new { LockoutStatus = "Locked" },
                new { LockoutStatus = "Unlocked" },
                ipAddress);

            TempData["SuccessMessage"] = $"Account for '{user.FullName}' has been unlocked successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var model = new ResetPasswordAdminViewModel
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordAdminViewModel model)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, err.Description);
                }
                return View(model);
            }

            await _userManager.UpdateSecurityStampAsync(user);

            await _auditService.LogAsync(
                currentUserId,
                AuditAction.PasswordChange,
                nameof(ApplicationUser),
                user.Id,
                null,
                new { Action = "Administrative Password Reset" },
                ipAddress);

            TempData["SuccessMessage"] = $"Password for '{user.FullName}' has been reset successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<IEnumerable<SelectListItem>> GetRolesSelectListAsync()
        {
            return await _roleManager.Roles.OrderBy(r => r.Name).Select(r => new SelectListItem
            {
                Value = r.Name,
                Text = r.Name
            }).ToListAsync();
        }
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Models.Constants;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class ActivitiesController : Controller
    {
        private readonly IActivityService _activityService;
        private readonly IUserScopeService _userScopeService;

        public ActivitiesController(
            IActivityService activityService,
            IUserScopeService userScopeService)
        {
            _activityService = activityService;
            _userScopeService = userScopeService;
        }

        public async Task<IActionResult> Index(
            string? type = null,
            string? status = null,
            string? assignedTo = null,
            string? search = null,
            string view = "table",
            int page = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var role = await _userScopeService.GetCurrentUserRoleAsync();

            var model = await _activityService.GetActivitiesAsync(
                userId,
                role,
                type,
                status,
                assignedTo,
                search,
                view,
                page,
                pageSize: 10);

            return View(model);
        }

        public async Task<IActionResult> Timeline(int? customerId = null, int? leadId = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var role = await _userScopeService.GetCurrentUserRoleAsync();

            var activities = await _activityService.GetTimelineAsync(userId, role, customerId, leadId, take: 50);

            ViewBag.CustomerId = customerId;
            ViewBag.LeadId = leadId;

            return View(activities);
        }

        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var role = await _userScopeService.GetCurrentUserRoleAsync();

            var activity = await _activityService.GetActivityByIdAsync(id, userId, role);
            if (activity == null)
            {
                return NotFound();
            }

            return View(activity);
        }

        public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var model = await _activityService.GetCreateViewModelAsync(customerId, leadId, userId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ActivityFormViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            if (!ModelState.IsValid)
            {
                var refreshed = await _activityService.GetCreateViewModelAsync(model.CustomerId, model.LeadId, model.AssignedTo);
                model.CustomersList = refreshed.CustomersList;
                model.LeadsList = refreshed.LeadsList;
                model.UsersList = refreshed.UsersList;
                model.TypesList = refreshed.TypesList;
                model.StatusesList = refreshed.StatusesList;
                return View(model);
            }

            var activity = await _activityService.CreateActivityAsync(model, userId, ipAddress);
            TempData["SuccessMessage"] = $"Activity '{activity.Subject}' successfully logged.";

            if (model.CustomerId.HasValue)
            {
                return RedirectToAction("Details", "Customers", new { id = model.CustomerId.Value });
            }
            if (model.LeadId.HasValue)
            {
                return RedirectToAction("Details", "Leads", new { id = model.LeadId.Value });
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var role = await _userScopeService.GetCurrentUserRoleAsync();

            var model = await _activityService.GetEditViewModelAsync(id, userId, role);
            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ActivityFormViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var role = await _userScopeService.GetCurrentUserRoleAsync();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            if (!ModelState.IsValid)
            {
                var refreshed = await _activityService.GetEditViewModelAsync(model.ActivityId, userId, role);
                if (refreshed != null)
                {
                    model.CustomersList = refreshed.CustomersList;
                    model.LeadsList = refreshed.LeadsList;
                    model.UsersList = refreshed.UsersList;
                    model.TypesList = refreshed.TypesList;
                    model.StatusesList = refreshed.StatusesList;
                }
                return View(model);
            }

            var updated = await _activityService.UpdateActivityAsync(model, userId, ipAddress);
            if (!updated)
            {
                return NotFound();
            }

            TempData["SuccessMessage"] = "Activity successfully updated.";
            return RedirectToAction(nameof(Details), new { id = model.ActivityId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id, string? completionNote, string? returnUrl = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var result = await _activityService.CompleteActivityAsync(id, completionNote, userId, ipAddress);
            if (!result)
            {
                return NotFound();
            }

            TempData["SuccessMessage"] = "Activity marked as Completed.";

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

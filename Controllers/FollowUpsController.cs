using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Models.Constants;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class FollowUpsController : Controller
    {
        private readonly IFollowUpService _followUpService;
        private readonly IUserScopeService _userScopeService;

        public FollowUpsController(
            IFollowUpService followUpService,
            IUserScopeService userScopeService)
        {
            _followUpService = followUpService;
            _userScopeService = userScopeService;
        }

        public async Task<IActionResult> Index(
            string filter = "All",
            string? type = null,
            string? assignedTo = null,
            string? search = null,
            int page = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var role = await _userScopeService.GetCurrentUserRoleAsync();

            var model = await _followUpService.GetFollowUpsAsync(
                userId,
                role,
                filter,
                type,
                assignedTo,
                search,
                page,
                pageSize: 10);

            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var role = await _userScopeService.GetCurrentUserRoleAsync();

            var followUp = await _followUpService.GetFollowUpByIdAsync(id, userId, role);
            if (followUp == null)
            {
                return NotFound();
            }

            return View(followUp);
        }

        public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var model = await _followUpService.GetCreateViewModelAsync(customerId, leadId, userId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FollowUpFormViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            if (!ModelState.IsValid)
            {
                var refreshed = await _followUpService.GetCreateViewModelAsync(model.CustomerId, model.LeadId, model.AssignedTo);
                model.CustomersList = refreshed.CustomersList;
                model.LeadsList = refreshed.LeadsList;
                model.UsersList = refreshed.UsersList;
                model.TypesList = refreshed.TypesList;
                return View(model);
            }

            var followUp = await _followUpService.CreateFollowUpAsync(model, userId, ipAddress);
            TempData["SuccessMessage"] = $"Follow-up successfully scheduled for {followUp.FollowUpDate:dd-MMM-yyyy HH:mm}.";

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

            var model = await _followUpService.GetEditViewModelAsync(id, userId, role);
            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FollowUpFormViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var role = await _userScopeService.GetCurrentUserRoleAsync();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            if (!ModelState.IsValid)
            {
                var refreshed = await _followUpService.GetEditViewModelAsync(model.FollowUpId, userId, role);
                if (refreshed != null)
                {
                    model.CustomersList = refreshed.CustomersList;
                    model.LeadsList = refreshed.LeadsList;
                    model.UsersList = refreshed.UsersList;
                    model.TypesList = refreshed.TypesList;
                }
                return View(model);
            }

            var updated = await _followUpService.UpdateFollowUpAsync(model, userId, ipAddress);
            if (!updated)
            {
                return NotFound();
            }

            TempData["SuccessMessage"] = "Follow-up successfully updated.";
            return RedirectToAction(nameof(Details), new { id = model.FollowUpId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id, string? remarks, string? returnUrl = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var result = await _followUpService.CompleteFollowUpAsync(id, remarks, userId, ipAddress);
            if (!result)
            {
                return NotFound();
            }

            TempData["SuccessMessage"] = "Follow-up marked as Completed.";

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reschedule(int id, DateTime newDate, string? remarks, string? returnUrl = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            if (newDate.Date < DateTime.UtcNow.Date)
            {
                TempData["ErrorMessage"] = "Follow-up date cannot be earlier than today.";
                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction(nameof(Details), new { id });
            }

            var result = await _followUpService.RescheduleFollowUpAsync(id, newDate, remarks, userId, ipAddress);
            if (!result)
            {
                return NotFound();
            }

            TempData["SuccessMessage"] = $"Follow-up successfully rescheduled to {newDate:dd-MMM-yyyy HH:mm}.";

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? remarks, string? returnUrl = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var result = await _followUpService.CancelFollowUpAsync(id, remarks, userId, ipAddress);
            if (!result)
            {
                return NotFound();
            }

            TempData["SuccessMessage"] = "Follow-up has been Cancelled.";

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

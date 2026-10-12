using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Models.Constants;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class LeadsController : Controller
    {
        private readonly ILeadService _leadService;
        private readonly IUserScopeService _userScopeService;

        public LeadsController(
            ILeadService leadService,
            IUserScopeService userScopeService)
        {
            _leadService = leadService;
            _userScopeService = userScopeService;
        }

        public async Task<IActionResult> Index(
            string? search,
            string? status,
            string? source,
            string? assignedTo,
            int page = 1)
        {
            var isElevatedRole = _userScopeService.IsAdmin() || _userScopeService.IsManager();
            var currentUserId = _userScopeService.GetCurrentUserId();

            var viewModel = await _leadService.GetLeadsAsync(
                search,
                status,
                source,
                assignedTo,
                page,
                pageSize: 10,
                currentUserId,
                isElevatedRole);

            return View(viewModel);
        }

        public async Task<IActionResult> Details(int id)
        {
            var lead = await _leadService.GetLeadByIdAsync(id);
            if (lead == null)
            {
                return NotFound();
            }

            if (!await _userScopeService.CanAccessRecordAsync(lead.AssignedTo))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View(lead);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new LeadFormViewModel
            {
                UsersList = await _leadService.GetAssignableUsersAsync(),
                AssignedTo = _userScopeService.GetCurrentUserId()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LeadFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.UsersList = await _leadService.GetAssignableUsersAsync();
                return View(model);
            }

            var currentUserId = _userScopeService.GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToAction("Login", "Account");
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, errorMessage, createdLead) = await _leadService.CreateLeadAsync(model, currentUserId, ip);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to create lead.");
                model.UsersList = await _leadService.GetAssignableUsersAsync();
                return View(model);
            }

            TempData["SuccessMessage"] = $"Lead '{createdLead?.LeadName}' created successfully ({createdLead?.LeadCode}).";
            return RedirectToAction(nameof(Details), new { id = createdLead?.LeadId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var lead = await _leadService.GetLeadByIdAsync(id);
            if (lead == null)
            {
                return NotFound();
            }

            if (!await _userScopeService.CanAccessRecordAsync(lead.AssignedTo))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (lead.Status == LeadStatus.Converted)
            {
                TempData["ErrorMessage"] = "Converted leads are permanently closed and cannot be edited.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var model = new LeadFormViewModel
            {
                LeadId = lead.LeadId,
                LeadCode = lead.LeadCode,
                LeadName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                Source = lead.Source,
                Status = lead.Status,
                ExpectedValue = lead.ExpectedValue,
                AssignedTo = lead.AssignedTo,
                UsersList = await _leadService.GetAssignableUsersAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(LeadFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.UsersList = await _leadService.GetAssignableUsersAsync();
                return View(model);
            }

            var lead = await _leadService.GetLeadByIdAsync(model.LeadId);
            if (lead == null)
            {
                return NotFound();
            }

            if (!await _userScopeService.CanAccessRecordAsync(lead.AssignedTo))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var currentUserId = _userScopeService.GetCurrentUserId() ?? "";
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var (success, errorMessage) = await _leadService.UpdateLeadAsync(model, currentUserId, ip);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to update lead.");
                model.UsersList = await _leadService.GetAssignableUsersAsync();
                return View(model);
            }

            TempData["SuccessMessage"] = $"Lead '{model.LeadName}' updated successfully.";
            return RedirectToAction(nameof(Details), new { id = model.LeadId });
        }

        [HttpGet]
        public async Task<IActionResult> Convert(int id)
        {
            var lead = await _leadService.GetLeadByIdAsync(id);
            if (lead == null)
            {
                return NotFound();
            }

            if (!await _userScopeService.CanAccessRecordAsync(lead.AssignedTo))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (lead.Status != LeadStatus.Qualified)
            {
                TempData["ErrorMessage"] = $"Only leads with 'Qualified' status can be converted. Current status is '{lead.Status}'.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var model = new LeadConvertViewModel
            {
                LeadId = lead.LeadId,
                LeadCode = lead.LeadCode,
                LeadName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                OpportunityName = $"{lead.LeadName} - Expansion Deal",
                DealAmount = lead.ExpectedValue > 0 ? lead.ExpectedValue : 50000,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(30),
                Probability = 30,
                Stage = OpportunityStage.Proposal
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Convert(LeadConvertViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var lead = await _leadService.GetLeadByIdAsync(model.LeadId);
            if (lead == null)
            {
                return NotFound();
            }

            if (!await _userScopeService.CanAccessRecordAsync(lead.AssignedTo))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var currentUserId = _userScopeService.GetCurrentUserId() ?? "";
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var (success, errorMessage, customer, opportunity) = await _leadService.ConvertLeadAsync(model, currentUserId, ip);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to convert lead.");
                return View(model);
            }

            TempData["SuccessMessage"] = $"Lead '{model.LeadCode}' successfully converted into Customer '{customer?.CustomerName}' ({customer?.CustomerCode}) and Opportunity '{opportunity?.OpportunityName}'!";
            return RedirectToAction(nameof(Details), new { id = model.LeadId });
        }
    }
}

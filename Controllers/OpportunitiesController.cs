using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Models.Constants;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class OpportunitiesController : Controller
    {
        private readonly IOpportunityService _opportunityService;
        private readonly IUserScopeService _userScopeService;

        public OpportunitiesController(
            IOpportunityService opportunityService,
            IUserScopeService userScopeService)
        {
            _opportunityService = opportunityService;
            _userScopeService = userScopeService;
        }

        public async Task<IActionResult> Index(
            string? search,
            string? stage,
            string? status,
            string? assignedTo,
            int page = 1)
        {
            var isElevatedRole = _userScopeService.IsAdmin() || _userScopeService.IsManager();
            var currentUserId = _userScopeService.GetCurrentUserId();

            var viewModel = await _opportunityService.GetOpportunitiesAsync(
                search,
                stage,
                status,
                assignedTo,
                page,
                pageSize: 10,
                currentUserId,
                isElevatedRole);

            return View(viewModel);
        }

        public async Task<IActionResult> Pipeline()
        {
            var isElevatedRole = _userScopeService.IsAdmin() || _userScopeService.IsManager();
            var currentUserId = _userScopeService.GetCurrentUserId();

            var viewModel = await _opportunityService.GetPipelineSummaryAsync(currentUserId, isElevatedRole);
            return View(viewModel);
        }

        public async Task<IActionResult> Details(int id)
        {
            var opportunity = await _opportunityService.GetOpportunityByIdAsync(id);
            if (opportunity == null)
            {
                return NotFound();
            }

            if (!await _userScopeService.CanAccessRecordAsync(opportunity.AssignedTo))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View(opportunity);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
        {
            var isElevatedRole = _userScopeService.IsAdmin() || _userScopeService.IsManager();
            var currentUserId = _userScopeService.GetCurrentUserId();

            var model = new OpportunityFormViewModel
            {
                CustomerId = customerId,
                LeadId = leadId,
                AssignedTo = currentUserId,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(30),
                CustomersList = await _opportunityService.GetCustomersDropdownAsync(currentUserId, isElevatedRole),
                LeadsList = await _opportunityService.GetLeadsDropdownAsync(currentUserId, isElevatedRole),
                UsersList = await _opportunityService.GetUsersDropdownAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OpportunityFormViewModel model)
        {
            var isElevatedRole = _userScopeService.IsAdmin() || _userScopeService.IsManager();
            var currentUserId = _userScopeService.GetCurrentUserId();

            if (!ModelState.IsValid)
            {
                model.CustomersList = await _opportunityService.GetCustomersDropdownAsync(currentUserId, isElevatedRole);
                model.LeadsList = await _opportunityService.GetLeadsDropdownAsync(currentUserId, isElevatedRole);
                model.UsersList = await _opportunityService.GetUsersDropdownAsync();
                return View(model);
            }

            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToAction("Login", "Account");
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, errorMessage, createdOpp) = await _opportunityService.CreateOpportunityAsync(model, currentUserId, ip);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to create opportunity.");
                model.CustomersList = await _opportunityService.GetCustomersDropdownAsync(currentUserId, isElevatedRole);
                model.LeadsList = await _opportunityService.GetLeadsDropdownAsync(currentUserId, isElevatedRole);
                model.UsersList = await _opportunityService.GetUsersDropdownAsync();
                return View(model);
            }

            TempData["SuccessMessage"] = $"Opportunity '{createdOpp?.OpportunityName}' created successfully.";
            return RedirectToAction(nameof(Details), new { id = createdOpp?.OpportunityId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var opportunity = await _opportunityService.GetOpportunityByIdAsync(id);
            if (opportunity == null)
            {
                return NotFound();
            }

            if (!await _userScopeService.CanAccessRecordAsync(opportunity.AssignedTo))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var isElevatedRole = _userScopeService.IsAdmin() || _userScopeService.IsManager();
            var currentUserId = _userScopeService.GetCurrentUserId();

            var model = new OpportunityFormViewModel
            {
                OpportunityId = opportunity.OpportunityId,
                OpportunityName = opportunity.OpportunityName,
                CustomerId = opportunity.CustomerId,
                LeadId = opportunity.LeadId,
                Amount = opportunity.Amount,
                Stage = opportunity.Stage,
                Probability = opportunity.Probability,
                ExpectedCloseDate = opportunity.ExpectedCloseDate,
                Status = opportunity.Status,
                AssignedTo = opportunity.AssignedTo,
                CustomersList = await _opportunityService.GetCustomersDropdownAsync(currentUserId, isElevatedRole),
                LeadsList = await _opportunityService.GetLeadsDropdownAsync(currentUserId, isElevatedRole),
                UsersList = await _opportunityService.GetUsersDropdownAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(OpportunityFormViewModel model)
        {
            var isElevatedRole = _userScopeService.IsAdmin() || _userScopeService.IsManager();
            var currentUserId = _userScopeService.GetCurrentUserId();

            if (!ModelState.IsValid)
            {
                model.CustomersList = await _opportunityService.GetCustomersDropdownAsync(currentUserId, isElevatedRole);
                model.LeadsList = await _opportunityService.GetLeadsDropdownAsync(currentUserId, isElevatedRole);
                model.UsersList = await _opportunityService.GetUsersDropdownAsync();
                return View(model);
            }

            var opportunity = await _opportunityService.GetOpportunityByIdAsync(model.OpportunityId);
            if (opportunity == null)
            {
                return NotFound();
            }

            if (!await _userScopeService.CanAccessRecordAsync(opportunity.AssignedTo))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, errorMessage) = await _opportunityService.UpdateOpportunityAsync(model, currentUserId ?? "", ip);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to update opportunity.");
                model.CustomersList = await _opportunityService.GetCustomersDropdownAsync(currentUserId, isElevatedRole);
                model.LeadsList = await _opportunityService.GetLeadsDropdownAsync(currentUserId, isElevatedRole);
                model.UsersList = await _opportunityService.GetUsersDropdownAsync();
                return View(model);
            }

            TempData["SuccessMessage"] = $"Opportunity '{model.OpportunityName}' updated successfully.";
            return RedirectToAction(nameof(Details), new { id = model.OpportunityId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStage(int id, string stage, string returnUrl = "Index")
        {
            var opportunity = await _opportunityService.GetOpportunityByIdAsync(id);
            if (opportunity == null)
            {
                return NotFound();
            }

            if (!await _userScopeService.CanAccessRecordAsync(opportunity.AssignedTo))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var currentUserId = _userScopeService.GetCurrentUserId() ?? "";
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            await _opportunityService.ChangeStageAsync(id, stage, currentUserId, ip);
            TempData["SuccessMessage"] = $"Deal '{opportunity.OpportunityName}' stage moved to {stage}.";

            if (returnUrl == "Pipeline")
            {
                return RedirectToAction(nameof(Pipeline));
            }
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}

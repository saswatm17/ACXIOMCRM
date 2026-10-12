using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class CustomersController : Controller
    {
        private readonly ICustomerService _customerService;
        private readonly IUserScopeService _userScopeService;

        public CustomersController(
            ICustomerService customerService,
            IUserScopeService userScopeService)
        {
            _customerService = customerService;
            _userScopeService = userScopeService;
        }

        public async Task<IActionResult> Index(string? search, string? status, int page = 1)
        {
            var isElevatedRole = _userScopeService.IsAdmin() || _userScopeService.IsManager();
            var currentUserId = _userScopeService.GetCurrentUserId();

            var viewModel = await _customerService.GetCustomersAsync(
                search,
                status,
                page,
                pageSize: 10,
                currentUserId,
                isElevatedRole);

            return View(viewModel);
        }

        public async Task<IActionResult> Details(int id)
        {
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            // Server-side ownership check: SalesExecutive cannot access another user's record
            if (!await _userScopeService.CanAccessRecordAsync(customer.CreatedBy))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View(customer);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CustomerFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var currentUserId = _userScopeService.GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToAction("Login", "Account");
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, errorMessage, createdCustomer) = await _customerService.CreateCustomerAsync(model, currentUserId, ip);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to create customer.");
                return View(model);
            }

            TempData["SuccessMessage"] = $"Customer '{createdCustomer?.CustomerName}' was created successfully ({createdCustomer?.CustomerCode}).";
            return RedirectToAction(nameof(Details), new { id = createdCustomer?.CustomerId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            if (!await _userScopeService.CanAccessRecordAsync(customer.CreatedBy))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var model = new CustomerFormViewModel
            {
                CustomerId = customer.CustomerId,
                CustomerCode = customer.CustomerCode,
                CustomerName = customer.CustomerName,
                Email = customer.Email,
                Phone = customer.Phone,
                CompanyName = customer.CompanyName,
                Address = customer.Address,
                City = customer.City,
                State = customer.State,
                Status = customer.Status
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CustomerFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var customer = await _customerService.GetCustomerByIdAsync(model.CustomerId);
            if (customer == null)
            {
                return NotFound();
            }

            if (!await _userScopeService.CanAccessRecordAsync(customer.CreatedBy))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var currentUserId = _userScopeService.GetCurrentUserId() ?? "";
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var (success, errorMessage) = await _customerService.UpdateCustomerAsync(model, currentUserId, ip);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to update customer.");
                return View(model);
            }

            TempData["SuccessMessage"] = $"Customer '{model.CustomerName}' details updated successfully.";
            return RedirectToAction(nameof(Details), new { id = model.CustomerId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            if (!await _userScopeService.CanAccessRecordAsync(customer.CreatedBy))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var currentUserId = _userScopeService.GetCurrentUserId() ?? "";
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            await _customerService.DeactivateCustomerAsync(id, currentUserId, ip);
            TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' has been deactivated.";
            return RedirectToAction(nameof(Index));
        }
    }
}

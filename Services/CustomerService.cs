using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Models.Constants;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public CustomerService(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<CustomerListViewModel> GetCustomersAsync(
            string? search,
            string? status,
            int page,
            int pageSize,
            string? currentUserId,
            bool isElevatedRole)
        {
            var query = _context.Customers
                .Include(c => c.CreatedByUser)
                .AsQueryable();

            // Scope filter: SalesExecutive only views their own created customers
            if (!isElevatedRole && !string.IsNullOrEmpty(currentUserId))
            {
                query = query.Where(c => c.CreatedBy == currentUserId);
            }

            // Status filter
            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query = query.Where(c => c.Status == status);
            }

            // Search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(c =>
                    c.CustomerName.ToLower().Contains(term) ||
                    c.Email.ToLower().Contains(term) ||
                    c.Phone.Contains(term) ||
                    (c.CompanyName != null && c.CompanyName.ToLower().Contains(term)) ||
                    (c.City != null && c.City.ToLower().Contains(term)));
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var items = await query
                .OrderByDescending(c => c.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new CustomerListViewModel
            {
                Customers = items,
                SearchTerm = search,
                SelectedStatus = status,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = pageSize
            };
        }

        public async Task<Customer?> GetCustomerByIdAsync(int id)
        {
            return await _context.Customers
                .Include(c => c.CreatedByUser)
                .Include(c => c.Leads)
                .Include(c => c.Opportunities)
                .Include(c => c.FollowUps)
                .Include(c => c.Activities)
                .FirstOrDefaultAsync(c => c.CustomerId == id);
        }

        public async Task<bool> IsEmailUniqueAsync(string email, int? excludeCustomerId = null)
        {
            var normalizedEmail = email.Trim().ToLower();
            return !await _context.Customers.AnyAsync(c =>
                c.Email.ToLower() == normalizedEmail &&
                (!excludeCustomerId.HasValue || c.CustomerId != excludeCustomerId.Value));
        }

        public async Task<bool> IsPhoneUniqueAsync(string phone, int? excludeCustomerId = null)
        {
            var cleanPhone = phone.Trim();
            return !await _context.Customers.AnyAsync(c =>
                c.Phone == cleanPhone &&
                (!excludeCustomerId.HasValue || c.CustomerId != excludeCustomerId.Value));
        }

        public async Task<(bool Success, string? ErrorMessage, Customer? Customer)> CreateCustomerAsync(
            CustomerFormViewModel model,
            string userId,
            string? ipAddress)
        {
            if (!await IsEmailUniqueAsync(model.Email))
            {
                return (false, "A customer with this email address already exists.", null);
            }

            if (!await IsPhoneUniqueAsync(model.Phone))
            {
                return (false, "A customer with this phone number already exists.", null);
            }

            var lastId = await _context.Customers
                .OrderByDescending(c => c.CustomerId)
                .Select(c => c.CustomerId)
                .FirstOrDefaultAsync();

            var customerCode = $"CUST-{1000 + lastId + 1}";

            var customer = new Customer
            {
                CustomerCode = customerCode,
                CustomerName = AcxiomCRM.Common.SanitizationHelper.SanitizeText(model.CustomerName) ?? string.Empty,
                Email = model.Email.Trim().ToLower(),
                Phone = model.Phone.Trim(),
                CompanyName = AcxiomCRM.Common.SanitizationHelper.SanitizeText(model.CompanyName),
                Address = AcxiomCRM.Common.SanitizationHelper.SanitizeText(model.Address),
                City = AcxiomCRM.Common.SanitizationHelper.SanitizeText(model.City),
                State = AcxiomCRM.Common.SanitizationHelper.SanitizeText(model.State),
                Status = CustomerStatus.Active,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = userId
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId,
                AuditAction.Create,
                nameof(Customer),
                customer.CustomerId.ToString(),
                null,
                new { customer.CustomerId, customer.CustomerCode, customer.CustomerName, customer.Email, customer.Phone },
                ipAddress);

            return (true, null, customer);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateCustomerAsync(
            CustomerFormViewModel model,
            string userId,
            string? ipAddress)
        {
            var customer = await _context.Customers.FindAsync(model.CustomerId);
            if (customer == null)
            {
                return (false, "Customer not found.");
            }

            if (!await IsEmailUniqueAsync(model.Email, model.CustomerId))
            {
                return (false, "A customer with this email address already exists.");
            }

            if (!await IsPhoneUniqueAsync(model.Phone, model.CustomerId))
            {
                return (false, "A customer with this phone number already exists.");
            }

            var oldSnapshot = new
            {
                customer.CustomerName,
                customer.Email,
                customer.Phone,
                customer.CompanyName,
                customer.Address,
                customer.City,
                customer.State,
                customer.Status
            };

            customer.CustomerName = AcxiomCRM.Common.SanitizationHelper.SanitizeText(model.CustomerName) ?? string.Empty;
            customer.Email = model.Email.Trim().ToLower();
            customer.Phone = model.Phone.Trim();
            customer.CompanyName = AcxiomCRM.Common.SanitizationHelper.SanitizeText(model.CompanyName);
            customer.Address = AcxiomCRM.Common.SanitizationHelper.SanitizeText(model.Address);
            customer.City = AcxiomCRM.Common.SanitizationHelper.SanitizeText(model.City);
            customer.State = AcxiomCRM.Common.SanitizationHelper.SanitizeText(model.State);
            customer.Status = model.Status;

            await _context.SaveChangesAsync();

            var newSnapshot = new
            {
                customer.CustomerName,
                customer.Email,
                customer.Phone,
                customer.CompanyName,
                customer.Address,
                customer.City,
                customer.State,
                customer.Status
            };

            await _auditService.LogAsync(
                userId,
                AuditAction.Update,
                nameof(Customer),
                customer.CustomerId.ToString(),
                oldSnapshot,
                newSnapshot,
                ipAddress);

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> DeactivateCustomerAsync(
            int id,
            string userId,
            string? ipAddress)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return (false, "Customer not found.");
            }

            var oldStatus = customer.Status;
            customer.Status = CustomerStatus.Inactive;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId,
                AuditAction.Delete,
                nameof(Customer),
                customer.CustomerId.ToString(),
                new { Status = oldStatus },
                new { Status = CustomerStatus.Inactive },
                ipAddress);

            return (true, null);
        }
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Common;
using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Models.Constants;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api
{
    [ApiController]
    [Route("api/customers")]
    [Produces("application/json")]
    public class CustomersApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public CustomersApiController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomerReadDto>>> GetCustomers(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _context.Customers.Include(c => c.CreatedByUser).AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(c => c.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(c =>
                    c.CustomerName.ToLower().Contains(term) ||
                    c.Email.ToLower().Contains(term) ||
                    c.Phone.Contains(term) ||
                    (c.CompanyName != null && c.CompanyName.ToLower().Contains(term)) ||
                    c.CustomerCode.ToLower().Contains(term));
            }

            var items = await query
                .OrderBy(c => c.CustomerName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CustomerReadDto
                {
                    CustomerId = c.CustomerId,
                    CustomerCode = c.CustomerCode,
                    CustomerName = c.CustomerName,
                    Email = c.Email,
                    Phone = c.Phone,
                    CompanyName = c.CompanyName,
                    Address = c.Address,
                    City = c.City,
                    State = c.State,
                    Status = c.Status,
                    CreatedDate = c.CreatedDate,
                    CreatedByName = c.CreatedByUser != null ? c.CreatedByUser.FullName : null
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CustomerReadDto>> GetCustomer(int id)
        {
            var customer = await _context.Customers
                .Include(c => c.CreatedByUser)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null)
            {
                return NotFound(new { message = $"Customer with ID {id} was not found." });
            }

            return Ok(new CustomerReadDto
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
                Status = customer.Status,
                CreatedDate = customer.CreatedDate,
                CreatedByName = customer.CreatedByUser?.FullName
            });
        }

        [HttpPost]
        public async Task<ActionResult<CustomerReadDto>> CreateCustomer([FromBody] CustomerCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var emailLower = dto.Email.Trim().ToLower();
            if (await _context.Customers.AnyAsync(c => c.Email.ToLower() == emailLower))
            {
                return BadRequest(new { message = "A customer with this email address already exists." });
            }

            var phoneTrim = dto.Phone.Trim();
            if (await _context.Customers.AnyAsync(c => c.Phone == phoneTrim))
            {
                return BadRequest(new { message = "A customer with this phone number already exists." });
            }

            var lastId = await _context.Customers.OrderByDescending(c => c.CustomerId).Select(c => c.CustomerId).FirstOrDefaultAsync();
            var customerCode = $"CUST-{1000 + lastId + 1}";

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var customer = new Customer
            {
                CustomerCode = customerCode,
                CustomerName = SanitizationHelper.SanitizeText(dto.CustomerName) ?? string.Empty,
                Email = emailLower,
                Phone = phoneTrim,
                CompanyName = SanitizationHelper.SanitizeText(dto.CompanyName),
                Address = SanitizationHelper.SanitizeText(dto.Address),
                City = SanitizationHelper.SanitizeText(dto.City),
                State = SanitizationHelper.SanitizeText(dto.State),
                Status = CustomerStatus.Active,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = currentUserId
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                AuditAction.Create,
                nameof(Customer),
                customer.CustomerId.ToString(),
                null,
                customer,
                ipAddress);

            var readDto = new CustomerReadDto
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
                Status = customer.Status,
                CreatedDate = customer.CreatedDate
            };

            return CreatedAtAction(nameof(GetCustomer), new { id = customer.CustomerId }, readDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] CustomerUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound(new { message = $"Customer with ID {id} was not found." });
            }

            var emailLower = dto.Email.Trim().ToLower();
            if (await _context.Customers.AnyAsync(c => c.Email.ToLower() == emailLower && c.CustomerId != id))
            {
                return BadRequest(new { message = "Another customer with this email address already exists." });
            }

            var phoneTrim = dto.Phone.Trim();
            if (await _context.Customers.AnyAsync(c => c.Phone == phoneTrim && c.CustomerId != id))
            {
                return BadRequest(new { message = "Another customer with this phone number already exists." });
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var oldSnapshot = new { customer.CustomerName, customer.Email, customer.Phone, customer.Status };

            customer.CustomerName = SanitizationHelper.SanitizeText(dto.CustomerName) ?? string.Empty;
            customer.Email = emailLower;
            customer.Phone = phoneTrim;
            customer.CompanyName = SanitizationHelper.SanitizeText(dto.CompanyName);
            customer.Address = SanitizationHelper.SanitizeText(dto.Address);
            customer.City = SanitizationHelper.SanitizeText(dto.City);
            customer.State = SanitizationHelper.SanitizeText(dto.State);
            customer.Status = dto.Status;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                AuditAction.Update,
                nameof(Customer),
                customer.CustomerId.ToString(),
                oldSnapshot,
                customer,
                ipAddress);

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound(new { message = $"Customer with ID {id} was not found." });
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            customer.Status = CustomerStatus.Inactive;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                AuditAction.Update,
                nameof(Customer),
                customer.CustomerId.ToString(),
                new { Status = CustomerStatus.Active },
                new { Status = CustomerStatus.Inactive },
                ipAddress);

            return NoContent();
        }
    }
}

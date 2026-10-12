using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Common;
using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Models.Constants;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers.Api
{
    [ApiController]
    [Route("api/leads")]
    [Produces("application/json")]
    public class LeadsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILeadService _leadService;
        private readonly IAuditService _auditService;

        public LeadsApiController(
            ApplicationDbContext context,
            ILeadService leadService,
            IAuditService auditService)
        {
            _context = context;
            _leadService = leadService;
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<LeadReadDto>>> GetLeads(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _context.Leads.Include(l => l.AssignedToUser).AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(l => l.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(l =>
                    l.LeadName.ToLower().Contains(term) ||
                    l.Email.ToLower().Contains(term) ||
                    l.Phone.Contains(term) ||
                    (l.CompanyName != null && l.CompanyName.ToLower().Contains(term)) ||
                    l.LeadCode.ToLower().Contains(term));
            }

            var items = await query
                .OrderByDescending(l => l.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new LeadReadDto
                {
                    LeadId = l.LeadId,
                    LeadCode = l.LeadCode,
                    LeadName = l.LeadName,
                    Email = l.Email,
                    Phone = l.Phone,
                    CompanyName = l.CompanyName,
                    Source = l.Source,
                    Status = l.Status,
                    ExpectedValue = l.ExpectedValue,
                    AssignedToUserId = l.AssignedTo,
                    AssignedToName = l.AssignedToUser != null ? l.AssignedToUser.FullName : null,
                    CreatedDate = l.CreatedDate
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<LeadReadDto>> GetLead(int id)
        {
            var lead = await _context.Leads
                .Include(l => l.AssignedToUser)
                .FirstOrDefaultAsync(l => l.LeadId == id);

            if (lead == null)
            {
                return NotFound(new { message = $"Lead with ID {id} was not found." });
            }

            return Ok(new LeadReadDto
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
                AssignedToUserId = lead.AssignedTo,
                AssignedToName = lead.AssignedToUser?.FullName,
                CreatedDate = lead.CreatedDate
            });
        }

        [HttpPost]
        public async Task<ActionResult<LeadReadDto>> CreateLead([FromBody] LeadCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var formModel = new LeadFormViewModel
            {
                LeadName = dto.LeadName,
                Email = dto.Email,
                Phone = dto.Phone,
                CompanyName = dto.CompanyName,
                Source = dto.Source,
                Status = LeadStatus.New,
                ExpectedValue = dto.ExpectedValue,
                AssignedTo = dto.AssignedTo
            };

            var (success, errorMessage, lead) = await _leadService.CreateLeadAsync(formModel, currentUserId ?? "api", ipAddress);
            if (!success || lead == null)
            {
                return BadRequest(new { message = errorMessage ?? "Unable to create lead." });
            }

            var readDto = new LeadReadDto
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
                AssignedToUserId = lead.AssignedTo,
                CreatedDate = lead.CreatedDate
            };

            return CreatedAtAction(nameof(GetLead), new { id = lead.LeadId }, readDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLead(int id, [FromBody] LeadUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var formModel = new LeadFormViewModel
            {
                LeadId = id,
                LeadName = dto.LeadName,
                Email = dto.Email,
                Phone = dto.Phone,
                CompanyName = dto.CompanyName,
                Source = dto.Source,
                Status = dto.Status,
                ExpectedValue = dto.ExpectedValue,
                AssignedTo = dto.AssignedTo
            };

            var (success, errorMessage) = await _leadService.UpdateLeadAsync(formModel, currentUserId ?? "api", ipAddress);
            if (!success)
            {
                return BadRequest(new { message = errorMessage ?? "Lead update failed." });
            }

            return NoContent();
        }

        [HttpPost("{id}/convert")]
        public async Task<IActionResult> ConvertLead(int id, [FromBody] LeadConvertApiDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "api";
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var convertModel = new LeadConvertViewModel
            {
                LeadId = id,
                OpportunityName = dto.OpportunityName,
                DealAmount = dto.OpportunityAmount,
                ExpectedCloseDate = dto.ExpectedCloseDate,
                Notes = "Converted via REST API."
            };

            var (success, errorMessage, customerId, opportunityId) = await _leadService.ConvertLeadAsync(convertModel, currentUserId, ipAddress);
            if (!success)
            {
                return BadRequest(new { message = errorMessage ?? "Lead conversion failed." });
            }

            return Ok(new
            {
                message = "Lead successfully converted to Customer and Opportunity.",
                customerId,
                opportunityId
            });
        }
    }
}

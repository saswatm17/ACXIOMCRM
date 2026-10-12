using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Models.Constants;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers.Api
{
    [ApiController]
    [Route("api/followups")]
    [Produces("application/json")]
    public class FollowUpsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IFollowUpService _followUpService;

        public FollowUpsApiController(
            ApplicationDbContext context,
            IFollowUpService followUpService)
        {
            _context = context;
            _followUpService = followUpService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<FollowUpReadDto>>> GetFollowUps(
            [FromQuery] string? status,
            [FromQuery] string? type,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            await _followUpService.AutoMarkOverdueAsync();

            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.AssignedToUser)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(f => f.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(f => f.FollowUpType == type);
            }

            var items = await query
                .OrderBy(f => f.FollowUpDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new FollowUpReadDto
                {
                    FollowUpId = f.FollowUpId,
                    CustomerId = f.CustomerId,
                    CustomerName = f.Customer != null ? f.Customer.CustomerName : null,
                    LeadId = f.LeadId,
                    LeadName = f.Lead != null ? f.Lead.LeadName : null,
                    FollowUpDate = f.FollowUpDate,
                    FollowUpType = f.FollowUpType,
                    Remarks = f.Remarks,
                    Status = f.Status,
                    AssignedToUserId = f.AssignedTo,
                    AssignedToName = f.AssignedToUser != null ? f.AssignedToUser.FullName : null,
                    CreatedDate = f.CreatedDate
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<FollowUpReadDto>> GetFollowUp(int id)
        {
            var f = await _context.FollowUps
                .Include(x => x.Customer)
                .Include(x => x.Lead)
                .Include(x => x.AssignedToUser)
                .FirstOrDefaultAsync(x => x.FollowUpId == id);

            if (f == null)
            {
                return NotFound(new { message = $"Follow-up with ID {id} was not found." });
            }

            return Ok(new FollowUpReadDto
            {
                FollowUpId = f.FollowUpId,
                CustomerId = f.CustomerId,
                CustomerName = f.Customer?.CustomerName,
                LeadId = f.LeadId,
                LeadName = f.Lead?.LeadName,
                FollowUpDate = f.FollowUpDate,
                FollowUpType = f.FollowUpType,
                Remarks = f.Remarks,
                Status = f.Status,
                AssignedToUserId = f.AssignedTo,
                AssignedToName = f.AssignedToUser?.FullName,
                CreatedDate = f.CreatedDate
            });
        }

        [HttpPost]
        public async Task<ActionResult<FollowUpReadDto>> CreateFollowUp([FromBody] FollowUpCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!dto.CustomerId.HasValue && !dto.LeadId.HasValue)
            {
                return BadRequest(new { message = "Follow-up must be associated with either a Customer or a Lead." });
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "api";
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var formModel = new FollowUpFormViewModel
            {
                CustomerId = dto.CustomerId,
                LeadId = dto.LeadId,
                FollowUpDate = dto.FollowUpDate,
                FollowUpType = dto.FollowUpType,
                Remarks = dto.Remarks,
                AssignedTo = dto.AssignedTo
            };

            var followUp = await _followUpService.CreateFollowUpAsync(formModel, currentUserId, ipAddress);

            var readDto = new FollowUpReadDto
            {
                FollowUpId = followUp.FollowUpId,
                CustomerId = followUp.CustomerId,
                LeadId = followUp.LeadId,
                FollowUpDate = followUp.FollowUpDate,
                FollowUpType = followUp.FollowUpType,
                Remarks = followUp.Remarks,
                Status = followUp.Status,
                AssignedToUserId = followUp.AssignedTo,
                CreatedDate = followUp.CreatedDate
            };

            return CreatedAtAction(nameof(GetFollowUp), new { id = followUp.FollowUpId }, readDto);
        }

        [HttpPut("{id}/complete")]
        public async Task<IActionResult> CompleteFollowUp(int id, [FromBody] FollowUpCompleteDto dto)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "api";
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var success = await _followUpService.CompleteFollowUpAsync(id, dto.Remarks, currentUserId, ipAddress);
            if (!success)
            {
                return NotFound(new { message = $"Follow-up with ID {id} was not found." });
            }

            return Ok(new { message = "Follow-up marked as Completed." });
        }

        [HttpPut("{id}/reschedule")]
        public async Task<IActionResult> RescheduleFollowUp(int id, [FromBody] FollowUpRescheduleDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (dto.NewDate.Date < DateTime.UtcNow.Date)
            {
                return BadRequest(new { message = "Follow-up date cannot be earlier than today." });
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "api";
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var success = await _followUpService.RescheduleFollowUpAsync(id, dto.NewDate, dto.Remarks, currentUserId, ipAddress);
            if (!success)
            {
                return NotFound(new { message = $"Follow-up with ID {id} was not found." });
            }

            return Ok(new { message = $"Follow-up rescheduled to {dto.NewDate:yyyy-MM-dd HH:mm}." });
        }
    }
}

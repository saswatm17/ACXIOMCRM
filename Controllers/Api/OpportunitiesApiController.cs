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
    [Route("api/opportunities")]
    [Produces("application/json")]
    public class OpportunitiesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IOpportunityService _opportunityService;

        public OpportunitiesApiController(
            ApplicationDbContext context,
            IOpportunityService opportunityService)
        {
            _context = context;
            _opportunityService = opportunityService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OpportunityReadDto>>> GetOpportunities(
            [FromQuery] string? stage,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.Lead)
                .Include(o => o.AssignedToUser)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(stage))
            {
                query = query.Where(o => o.Stage == stage);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.Status == status);
            }

            var items = await query
                .OrderByDescending(o => o.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OpportunityReadDto
                {
                    OpportunityId = o.OpportunityId,
                    OpportunityName = o.OpportunityName,
                    CustomerId = o.CustomerId,
                    CustomerName = o.Customer != null ? o.Customer.CustomerName : null,
                    LeadId = o.LeadId,
                    LeadName = o.Lead != null ? o.Lead.LeadName : null,
                    Amount = o.Amount,
                    Stage = o.Stage,
                    Probability = o.Probability,
                    WeightedAmount = (o.Amount * o.Probability) / 100m,
                    ExpectedCloseDate = o.ExpectedCloseDate,
                    Status = o.Status,
                    AssignedToUserId = o.AssignedTo,
                    AssignedToName = o.AssignedToUser != null ? o.AssignedToUser.FullName : null,
                    CreatedDate = o.CreatedDate
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<OpportunityReadDto>> GetOpportunity(int id)
        {
            var o = await _context.Opportunities
                .Include(x => x.Customer)
                .Include(x => x.Lead)
                .Include(x => x.AssignedToUser)
                .FirstOrDefaultAsync(x => x.OpportunityId == id);

            if (o == null)
            {
                return NotFound(new { message = $"Opportunity with ID {id} was not found." });
            }

            return Ok(new OpportunityReadDto
            {
                OpportunityId = o.OpportunityId,
                OpportunityName = o.OpportunityName,
                CustomerId = o.CustomerId,
                CustomerName = o.Customer?.CustomerName,
                LeadId = o.LeadId,
                LeadName = o.Lead?.LeadName,
                Amount = o.Amount,
                Stage = o.Stage,
                Probability = o.Probability,
                WeightedAmount = (o.Amount * o.Probability) / 100m,
                ExpectedCloseDate = o.ExpectedCloseDate,
                Status = o.Status,
                AssignedToUserId = o.AssignedTo,
                AssignedToName = o.AssignedToUser?.FullName,
                CreatedDate = o.CreatedDate
            });
        }

        [HttpPost]
        public async Task<ActionResult<OpportunityReadDto>> CreateOpportunity([FromBody] OpportunityCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "api";
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var formModel = new OpportunityFormViewModel
            {
                OpportunityName = dto.OpportunityName,
                CustomerId = dto.CustomerId,
                LeadId = dto.LeadId,
                Amount = dto.Amount,
                Stage = dto.Stage,
                Probability = dto.Probability,
                ExpectedCloseDate = dto.ExpectedCloseDate,
                AssignedTo = dto.AssignedTo
            };

            var (success, errorMessage, opp) = await _opportunityService.CreateOpportunityAsync(formModel, currentUserId, ipAddress);
            if (!success || opp == null)
            {
                return BadRequest(new { message = errorMessage ?? "Unable to create opportunity." });
            }

            var readDto = new OpportunityReadDto
            {
                OpportunityId = opp.OpportunityId,
                OpportunityName = opp.OpportunityName,
                CustomerId = opp.CustomerId,
                LeadId = opp.LeadId,
                Amount = opp.Amount,
                Stage = opp.Stage,
                Probability = opp.Probability,
                WeightedAmount = (opp.Amount * opp.Probability) / 100m,
                ExpectedCloseDate = opp.ExpectedCloseDate,
                Status = opp.Status,
                AssignedToUserId = opp.AssignedTo,
                CreatedDate = opp.CreatedDate
            };

            return CreatedAtAction(nameof(GetOpportunity), new { id = opp.OpportunityId }, readDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOpportunity(int id, [FromBody] OpportunityUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "api";
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var formModel = new OpportunityFormViewModel
            {
                OpportunityId = id,
                OpportunityName = dto.OpportunityName,
                CustomerId = dto.CustomerId,
                LeadId = dto.LeadId,
                Amount = dto.Amount,
                Stage = dto.Stage,
                Probability = dto.Probability,
                ExpectedCloseDate = dto.ExpectedCloseDate,
                AssignedTo = dto.AssignedTo
            };

            var (success, errorMessage) = await _opportunityService.UpdateOpportunityAsync(formModel, currentUserId, ipAddress);
            if (!success)
            {
                return BadRequest(new { message = errorMessage ?? "Opportunity update failed." });
            }

            return NoContent();
        }

        [HttpPut("{id}/stage")]
        public async Task<IActionResult> ChangeStage(int id, [FromBody] ChangeStageApiDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "api";
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var (success, errorMessage) = await _opportunityService.ChangeStageAsync(id, dto.Stage, currentUserId, ipAddress);
            if (!success)
            {
                return BadRequest(new { message = errorMessage ?? "Stage change failed." });
            }

            return Ok(new { message = $"Opportunity stage changed to {dto.Stage}." });
        }
    }
}

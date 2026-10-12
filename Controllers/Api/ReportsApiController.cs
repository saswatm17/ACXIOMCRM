using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models.Constants;

namespace AcxiomCRM.Controllers.Api
{
    [ApiController]
    [Route("api/reports")]
    [Produces("application/json")]
    public class ReportsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReportsApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("pipeline")]
        public async Task<ActionResult<PipelineReportSummaryDto>> GetPipelineSummary()
        {
            var deals = await _context.Opportunities.ToListAsync();

            var activeDeals = deals.Where(d => d.Status == OpportunityStatus.Open).ToList();
            var wonDeals = deals.Where(d => d.Status == OpportunityStatus.Won || d.Stage == OpportunityStage.Won).ToList();

            var breakdown = new List<StageSummaryDto>();

            foreach (var stage in OpportunityStage.All)
            {
                var stageDeals = deals.Where(d => d.Stage == stage).ToList();
                breakdown.Add(new StageSummaryDto
                {
                    StageName = stage,
                    DealsCount = stageDeals.Count,
                    TotalAmount = stageDeals.Sum(d => d.Amount),
                    WeightedAmount = stageDeals.Sum(d => (d.Amount * d.Probability) / 100m)
                });
            }

            var summary = new PipelineReportSummaryDto
            {
                TotalDealsCount = deals.Count,
                TotalPipelineValue = activeDeals.Sum(d => d.Amount),
                TotalWeightedValue = activeDeals.Sum(d => (d.Amount * d.Probability) / 100m),
                TotalWonRevenue = wonDeals.Sum(d => d.Amount),
                StageBreakdown = breakdown
            };

            return Ok(summary);
        }
    }
}

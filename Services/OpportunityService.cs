using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Models.Constants;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Services
{
    public class OpportunityService : IOpportunityService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public OpportunityService(
            ApplicationDbContext context,
            IAuditService auditService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _auditService = auditService;
            _userManager = userManager;
        }

        public async Task<OpportunityListViewModel> GetOpportunitiesAsync(
            string? search,
            string? stage,
            string? status,
            string? assignedTo,
            int page,
            int pageSize,
            string? currentUserId,
            bool isElevatedRole)
        {
            var query = _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.Lead)
                .Include(o => o.AssignedToUser)
                .AsQueryable();

            // Role scoping: Sales reps only see their assigned deals
            if (!isElevatedRole && !string.IsNullOrEmpty(currentUserId))
            {
                query = query.Where(o => o.AssignedTo == currentUserId);
            }

            if (!string.IsNullOrWhiteSpace(stage) && stage != "All")
            {
                query = query.Where(o => o.Stage == stage);
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query = query.Where(o => o.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(assignedTo) && assignedTo != "All")
            {
                query = query.Where(o => o.AssignedTo == assignedTo);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(o =>
                    o.OpportunityName.ToLower().Contains(term) ||
                    (o.Customer != null && o.Customer.CustomerName.ToLower().Contains(term)) ||
                    (o.Lead != null && o.Lead.LeadName.ToLower().Contains(term)));
            }

            var allMatching = await query.ToListAsync();

            var totalCount = allMatching.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var items = allMatching
                .OrderByDescending(o => o.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var openDeals = allMatching.Where(o => o.Status == OpportunityStatus.Open).ToList();
            var wonDeals = allMatching.Where(o => o.Status == OpportunityStatus.Won).ToList();

            var totalPipelineValue = openDeals.Sum(o => o.Amount);
            var totalWeightedValue = openDeals.Sum(o => (o.Amount * o.Probability) / 100m);
            var wonValue = wonDeals.Sum(o => o.Amount);

            return new OpportunityListViewModel
            {
                Opportunities = items,
                SearchTerm = search,
                SelectedStage = stage,
                SelectedStatus = status,
                SelectedAssignedTo = assignedTo,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = pageSize,
                TotalPipelineValue = totalPipelineValue,
                TotalWeightedValue = totalWeightedValue,
                WonValue = wonValue,
                OpenDealsCount = openDeals.Count,
                WonDealsCount = wonDeals.Count,
                UsersList = await GetUsersDropdownAsync()
            };
        }

        public async Task<OpportunityPipelineViewModel> GetPipelineSummaryAsync(string? currentUserId, bool isElevatedRole)
        {
            var query = _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.AssignedToUser)
                .AsQueryable();

            if (!isElevatedRole && !string.IsNullOrEmpty(currentUserId))
            {
                query = query.Where(o => o.AssignedTo == currentUserId);
            }

            var allDeals = await query.ToListAsync();

            var stagesSummary = new List<StagePipelineSummary>();

            foreach (var stageName in OpportunityStage.All)
            {
                var stageDeals = allDeals.Where(o => o.Stage == stageName).ToList();
                var totalAmount = stageDeals.Sum(o => o.Amount);
                var weightedAmount = stageDeals.Sum(o => (o.Amount * o.Probability) / 100m);

                stagesSummary.Add(new StagePipelineSummary
                {
                    StageName = stageName,
                    DealsCount = stageDeals.Count,
                    TotalAmount = totalAmount,
                    WeightedAmount = weightedAmount,
                    Deals = stageDeals
                });
            }

            var activeDeals = allDeals.Where(o => o.Status == OpportunityStatus.Open).ToList();

            return new OpportunityPipelineViewModel
            {
                Stages = stagesSummary,
                TotalPipelineValue = activeDeals.Sum(o => o.Amount),
                TotalWeightedValue = activeDeals.Sum(o => (o.Amount * o.Probability) / 100m),
                TotalDealsCount = allDeals.Count
            };
        }

        public async Task<Opportunity?> GetOpportunityByIdAsync(int id)
        {
            return await _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.Lead)
                .Include(o => o.AssignedToUser)
                .FirstOrDefaultAsync(o => o.OpportunityId == id);
        }

        public async Task<(bool Success, string? ErrorMessage, Opportunity? Opportunity)> CreateOpportunityAsync(
            OpportunityFormViewModel model,
            string currentUserId,
            string? ipAddress)
        {
            if (model.Amount <= 0)
            {
                return (false, "Opportunity Amount must be greater than 0.", null);
            }

            if (model.Probability < 0 || model.Probability > 100)
            {
                return (false, "Probability must be between 0 and 100.", null);
            }

            if (model.Stage != OpportunityStage.Won && model.Stage != OpportunityStage.Lost)
            {
                if (model.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
                {
                    return (false, "Expected Close Date cannot be in the past.", null);
                }
            }

            var status = model.Stage switch
            {
                OpportunityStage.Won => OpportunityStatus.Won,
                OpportunityStage.Lost => OpportunityStatus.Lost,
                _ => OpportunityStatus.Open
            };

            var probability = model.Stage switch
            {
                OpportunityStage.Won => 100,
                OpportunityStage.Lost => 0,
                _ => model.Probability
            };

            var assignedTo = string.IsNullOrWhiteSpace(model.AssignedTo)
                ? currentUserId
                : model.AssignedTo;

            var opportunity = new Opportunity
            {
                OpportunityName = AcxiomCRM.Common.SanitizationHelper.SanitizeText(model.OpportunityName) ?? string.Empty,
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                Amount = model.Amount,
                Stage = model.Stage,
                Probability = probability,
                ExpectedCloseDate = model.ExpectedCloseDate,
                Status = status,
                CreatedDate = DateTime.UtcNow,
                AssignedTo = assignedTo
            };

            _context.Opportunities.Add(opportunity);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                AuditAction.Create,
                nameof(Opportunity),
                opportunity.OpportunityId.ToString(),
                null,
                new { opportunity.OpportunityId, opportunity.OpportunityName, opportunity.Amount, opportunity.Stage, opportunity.Probability },
                ipAddress);

            return (true, null, opportunity);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateOpportunityAsync(
            OpportunityFormViewModel model,
            string currentUserId,
            string? ipAddress)
        {
            var opportunity = await _context.Opportunities.FindAsync(model.OpportunityId);
            if (opportunity == null)
            {
                return (false, "Opportunity not found.");
            }

            if (model.Amount <= 0)
            {
                return (false, "Opportunity Amount must be greater than 0.");
            }

            if (model.Probability < 0 || model.Probability > 100)
            {
                return (false, "Probability must be between 0 and 100.");
            }

            if (model.Stage != OpportunityStage.Won && model.Stage != OpportunityStage.Lost)
            {
                if (model.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
                {
                    return (false, "Expected Close Date cannot be in the past.");
                }
            }

            var oldSnapshot = new
            {
                opportunity.OpportunityName,
                opportunity.CustomerId,
                opportunity.LeadId,
                opportunity.Amount,
                opportunity.Stage,
                opportunity.Probability,
                opportunity.ExpectedCloseDate,
                opportunity.Status,
                opportunity.AssignedTo
            };

            var status = model.Stage switch
            {
                OpportunityStage.Won => OpportunityStatus.Won,
                OpportunityStage.Lost => OpportunityStatus.Lost,
                _ => OpportunityStatus.Open
            };

            var probability = model.Stage switch
            {
                OpportunityStage.Won => 100,
                OpportunityStage.Lost => 0,
                _ => model.Probability
            };

            opportunity.OpportunityName = AcxiomCRM.Common.SanitizationHelper.SanitizeText(model.OpportunityName) ?? string.Empty;
            opportunity.CustomerId = model.CustomerId;
            opportunity.LeadId = model.LeadId;
            opportunity.Amount = model.Amount;
            opportunity.Stage = model.Stage;
            opportunity.Probability = probability;
            opportunity.ExpectedCloseDate = model.ExpectedCloseDate;
            opportunity.Status = status;

            if (!string.IsNullOrWhiteSpace(model.AssignedTo))
            {
                opportunity.AssignedTo = model.AssignedTo;
            }

            await _context.SaveChangesAsync();

            var newSnapshot = new
            {
                opportunity.OpportunityName,
                opportunity.CustomerId,
                opportunity.LeadId,
                opportunity.Amount,
                opportunity.Stage,
                opportunity.Probability,
                opportunity.ExpectedCloseDate,
                opportunity.Status,
                opportunity.AssignedTo
            };

            await _auditService.LogAsync(
                currentUserId,
                AuditAction.Update,
                nameof(Opportunity),
                opportunity.OpportunityId.ToString(),
                oldSnapshot,
                newSnapshot,
                ipAddress);

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> ChangeStageAsync(
            int id,
            string newStage,
            string currentUserId,
            string? ipAddress)
        {
            var opportunity = await _context.Opportunities.FindAsync(id);
            if (opportunity == null)
            {
                return (false, "Opportunity not found.");
            }

            var oldStage = opportunity.Stage;
            var oldStatus = opportunity.Status;
            var oldProbability = opportunity.Probability;

            opportunity.Stage = newStage;
            opportunity.Status = newStage switch
            {
                OpportunityStage.Won => OpportunityStatus.Won,
                OpportunityStage.Lost => OpportunityStatus.Lost,
                _ => OpportunityStatus.Open
            };

            if (newStage == OpportunityStage.Won)
            {
                opportunity.Probability = 100;
            }
            else if (newStage == OpportunityStage.Lost)
            {
                opportunity.Probability = 0;
            }

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                AuditAction.Update,
                nameof(Opportunity),
                opportunity.OpportunityId.ToString(),
                new { Stage = oldStage, Status = oldStatus, Probability = oldProbability },
                new { Stage = opportunity.Stage, Status = opportunity.Status, Probability = opportunity.Probability },
                ipAddress);

            return (true, null);
        }

        public async Task<IEnumerable<SelectListItem>> GetCustomersDropdownAsync(string? currentUserId, bool isElevatedRole)
        {
            var query = _context.Customers.Where(c => c.Status == CustomerStatus.Active).AsQueryable();
            if (!isElevatedRole && !string.IsNullOrEmpty(currentUserId))
            {
                query = query.Where(c => c.CreatedBy == currentUserId);
            }

            return await query
                .OrderBy(c => c.CustomerName)
                .Select(c => new SelectListItem
                {
                    Value = c.CustomerId.ToString(),
                    Text = $"{c.CustomerName} ({c.CustomerCode})"
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<SelectListItem>> GetLeadsDropdownAsync(string? currentUserId, bool isElevatedRole)
        {
            var query = _context.Leads.AsQueryable();
            if (!isElevatedRole && !string.IsNullOrEmpty(currentUserId))
            {
                query = query.Where(l => l.AssignedTo == currentUserId);
            }

            return await query
                .OrderByDescending(l => l.CreatedDate)
                .Select(l => new SelectListItem
                {
                    Value = l.LeadId.ToString(),
                    Text = $"{l.LeadName} ({l.LeadCode} - {l.Status})"
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<SelectListItem>> GetUsersDropdownAsync()
        {
            return await _userManager.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.FullName)
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = $"{u.FullName} ({u.Email})"
                })
                .ToListAsync();
        }
    }
}

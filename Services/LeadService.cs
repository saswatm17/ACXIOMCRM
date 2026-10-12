using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Models.Constants;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Services
{
    public class LeadService : ILeadService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public LeadService(
            ApplicationDbContext context,
            IAuditService auditService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _auditService = auditService;
            _userManager = userManager;
        }

        public bool IsValidStatusTransition(string currentStatus, string newStatus)
        {
            if (string.Equals(currentStatus, newStatus, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return currentStatus switch
            {
                LeadStatus.New => newStatus is LeadStatus.Contacted or LeadStatus.Unqualified or LeadStatus.Lost,
                LeadStatus.Contacted => newStatus is LeadStatus.Qualified or LeadStatus.Unqualified or LeadStatus.Lost,
                LeadStatus.Qualified => newStatus is LeadStatus.Converted or LeadStatus.Lost,
                LeadStatus.Unqualified => newStatus is LeadStatus.Contacted,
                LeadStatus.Lost => newStatus is LeadStatus.Contacted,
                LeadStatus.Converted => false, // Terminal state: cannot change once converted
                _ => false
            };
        }

        public async Task<LeadListViewModel> GetLeadsAsync(
            string? search,
            string? status,
            string? source,
            string? assignedTo,
            int page,
            int pageSize,
            string? currentUserId,
            bool isElevatedRole)
        {
            var query = _context.Leads
                .Include(l => l.AssignedToUser)
                .Include(l => l.Customer)
                .AsQueryable();

            // Scope filter: SalesExecutive only sees their assigned leads
            if (!isElevatedRole && !string.IsNullOrEmpty(currentUserId))
            {
                query = query.Where(l => l.AssignedTo == currentUserId);
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query = query.Where(l => l.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(source) && source != "All")
            {
                query = query.Where(l => l.Source == source);
            }

            if (!string.IsNullOrWhiteSpace(assignedTo) && assignedTo != "All")
            {
                query = query.Where(l => l.AssignedTo == assignedTo);
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

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var items = await query
                .OrderByDescending(l => l.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var usersList = await GetAssignableUsersAsync();

            return new LeadListViewModel
            {
                Leads = items,
                SearchTerm = search,
                SelectedStatus = status,
                SelectedSource = source,
                SelectedAssignedTo = assignedTo,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = pageSize,
                UsersList = usersList
            };
        }

        public async Task<Lead?> GetLeadByIdAsync(int id)
        {
            return await _context.Leads
                .Include(l => l.AssignedToUser)
                .Include(l => l.Customer)
                .Include(l => l.Opportunities)
                .Include(l => l.FollowUps)
                .Include(l => l.Activities)
                .FirstOrDefaultAsync(l => l.LeadId == id);
        }

        public async Task<IEnumerable<SelectListItem>> GetAssignableUsersAsync()
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

        public async Task<(bool Success, string? ErrorMessage, Lead? Lead)> CreateLeadAsync(
            LeadFormViewModel model,
            string currentUserId,
            string? ipAddress)
        {
            var lastId = await _context.Leads
                .OrderByDescending(l => l.LeadId)
                .Select(l => l.LeadId)
                .FirstOrDefaultAsync();

            var leadCode = $"LEAD-{1000 + lastId + 1}";

            var assignedTo = string.IsNullOrWhiteSpace(model.AssignedTo)
                ? currentUserId
                : model.AssignedTo;

            var lead = new Lead
            {
                LeadCode = leadCode,
                LeadName = AcxiomCRM.Common.SanitizationHelper.SanitizeText(model.LeadName) ?? string.Empty,
                Email = model.Email.Trim().ToLower(),
                Phone = model.Phone.Trim(),
                CompanyName = AcxiomCRM.Common.SanitizationHelper.SanitizeText(model.CompanyName),
                Source = model.Source,
                Status = LeadStatus.New,
                ExpectedValue = model.ExpectedValue,
                CreatedDate = DateTime.UtcNow,
                AssignedTo = assignedTo
            };

            _context.Leads.Add(lead);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                AuditAction.Create,
                nameof(Lead),
                lead.LeadId.ToString(),
                null,
                new { lead.LeadId, lead.LeadCode, lead.LeadName, lead.Email, lead.Status, lead.ExpectedValue, lead.AssignedTo },
                ipAddress);

            return (true, null, lead);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateLeadAsync(
            LeadFormViewModel model,
            string currentUserId,
            string? ipAddress)
        {
            var lead = await _context.Leads.FindAsync(model.LeadId);
            if (lead == null)
            {
                return (false, "Lead not found.");
            }

            if (lead.Status == LeadStatus.Converted && model.Status != LeadStatus.Converted)
            {
                return (false, "Converted leads cannot have their status altered.");
            }

            // Validate status workflow transition
            if (!string.Equals(lead.Status, model.Status, StringComparison.OrdinalIgnoreCase))
            {
                if (!IsValidStatusTransition(lead.Status, model.Status))
                {
                    return (false, $"Invalid status transition from '{lead.Status}' to '{model.Status}'. Follow the lead qualification workflow.");
                }
            }

            var oldSnapshot = new
            {
                lead.LeadName,
                lead.Email,
                lead.Phone,
                lead.CompanyName,
                lead.Source,
                lead.Status,
                lead.ExpectedValue,
                lead.AssignedTo
            };

            lead.LeadName = AcxiomCRM.Common.SanitizationHelper.SanitizeText(model.LeadName) ?? string.Empty;
            lead.Email = model.Email.Trim().ToLower();
            lead.Phone = model.Phone.Trim();
            lead.CompanyName = AcxiomCRM.Common.SanitizationHelper.SanitizeText(model.CompanyName);
            lead.Source = model.Source;
            lead.Status = model.Status;
            lead.ExpectedValue = model.ExpectedValue;

            if (!string.IsNullOrWhiteSpace(model.AssignedTo))
            {
                lead.AssignedTo = model.AssignedTo;
            }

            await _context.SaveChangesAsync();

            var newSnapshot = new
            {
                lead.LeadName,
                lead.Email,
                lead.Phone,
                lead.CompanyName,
                lead.Source,
                lead.Status,
                lead.ExpectedValue,
                lead.AssignedTo
            };

            await _auditService.LogAsync(
                currentUserId,
                AuditAction.Update,
                nameof(Lead),
                lead.LeadId.ToString(),
                oldSnapshot,
                newSnapshot,
                ipAddress);

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage, Customer? Customer, Opportunity? Opportunity)> ConvertLeadAsync(
            LeadConvertViewModel model,
            string currentUserId,
            string? ipAddress)
        {
            var lead = await _context.Leads.FindAsync(model.LeadId);
            if (lead == null)
            {
                return (false, "Lead not found.", null, null);
            }

            if (lead.Status != LeadStatus.Qualified)
            {
                return (false, "Only leads with 'Qualified' status can be converted into Customers and Opportunities.", null, null);
            }

            if (model.DealAmount <= 0)
            {
                return (false, "Opportunity Amount must be greater than 0.", null, null);
            }

            if (model.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
            {
                return (false, "Expected Close Date cannot be in the past.", null, null);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Check if Customer already exists with same email, or create new Customer
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email.ToLower() == lead.Email.ToLower());
                if (customer == null)
                {
                    var lastCustomerId = await _context.Customers
                        .OrderByDescending(c => c.CustomerId)
                        .Select(c => c.CustomerId)
                        .FirstOrDefaultAsync();

                    customer = new Customer
                    {
                        CustomerCode = $"CUST-{1000 + lastCustomerId + 1}",
                        CustomerName = string.IsNullOrWhiteSpace(lead.CompanyName) ? lead.LeadName : lead.CompanyName,
                        Email = lead.Email,
                        Phone = lead.Phone,
                        CompanyName = lead.CompanyName,
                        Status = CustomerStatus.Active,
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = lead.AssignedTo ?? currentUserId
                    };

                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync();
                }

                // Create new Opportunity linked to Customer and Lead
                var opportunity = new Opportunity
                {
                    OpportunityName = model.OpportunityName.Trim(),
                    CustomerId = customer.CustomerId,
                    LeadId = lead.LeadId,
                    Amount = model.DealAmount,
                    Stage = model.Stage,
                    Probability = model.Probability,
                    ExpectedCloseDate = model.ExpectedCloseDate,
                    Status = OpportunityStatus.Open,
                    CreatedDate = DateTime.UtcNow,
                    AssignedTo = lead.AssignedTo ?? currentUserId
                };

                _context.Opportunities.Add(opportunity);
                await _context.SaveChangesAsync();

                // Update Lead to Converted and link customer
                var oldStatus = lead.Status;
                lead.Status = LeadStatus.Converted;
                lead.CustomerId = customer.CustomerId;

                await _context.SaveChangesAsync();

                // Log Lead Conversion Audit
                await _auditService.LogAsync(
                    currentUserId,
                    AuditAction.LeadConversion,
                    nameof(Lead),
                    lead.LeadId.ToString(),
                    new { Status = oldStatus },
                    new
                    {
                        Status = LeadStatus.Converted,
                        CustomerId = customer.CustomerId,
                        CustomerCode = customer.CustomerCode,
                        OpportunityId = opportunity.OpportunityId,
                        OpportunityAmount = opportunity.Amount
                    },
                    ipAddress);

                await transaction.CommitAsync();
                return (true, null, customer, opportunity);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Error converting lead: {ex.Message}", null, null);
            }
        }
    }
}

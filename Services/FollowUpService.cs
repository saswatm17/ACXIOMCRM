using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Models.Constants;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Services
{
    public class FollowUpService : IFollowUpService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserScopeService _userScopeService;
        private readonly IAuditService _auditService;

        public FollowUpService(
            ApplicationDbContext context,
            IUserScopeService userScopeService,
            IAuditService auditService)
        {
            _context = context;
            _userScopeService = userScopeService;
            _auditService = auditService;
        }

        public async Task<FollowUpListViewModel> GetFollowUpsAsync(
            string currentUserId,
            string role,
            string? filter,
            string? type,
            string? assignedTo,
            string? search,
            int page = 1,
            int pageSize = 10)
        {
            // Auto-mark overdue follow-ups
            await AutoMarkOverdueAsync();

            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.AssignedToUser)
                .AsQueryable();

            if (role == AppRoles.SalesExecutive)
            {
                query = query.Where(f => f.AssignedTo == currentUserId);
            }

            var now = DateTime.UtcNow;
            var today = now.Date;

            // Aggregated counts for KPI cards
            var baseScoped = query;
            int totalCount = await baseScoped.CountAsync();
            int todayCount = await baseScoped.CountAsync(f => f.FollowUpDate.Date == today && f.Status == FollowUpStatus.Planned);
            int upcomingCount = await baseScoped.CountAsync(f => f.FollowUpDate > now && f.Status == FollowUpStatus.Planned);
            int overdueCount = await baseScoped.CountAsync(f => f.Status == FollowUpStatus.Missed || (f.FollowUpDate < now && f.Status == FollowUpStatus.Planned));
            int completedCount = await baseScoped.CountAsync(f => f.Status == FollowUpStatus.Completed);

            // Filter tab logic
            filter = string.IsNullOrWhiteSpace(filter) ? "All" : filter;
            switch (filter.ToLower())
            {
                case "today":
                    query = query.Where(f => f.FollowUpDate.Date == today);
                    break;
                case "upcoming":
                    query = query.Where(f => f.FollowUpDate > now && f.Status == FollowUpStatus.Planned);
                    break;
                case "overdue":
                    query = query.Where(f => f.Status == FollowUpStatus.Missed || (f.FollowUpDate < now && f.Status == FollowUpStatus.Planned));
                    break;
                case "completed":
                    query = query.Where(f => f.Status == FollowUpStatus.Completed);
                    break;
                case "missed":
                    query = query.Where(f => f.Status == FollowUpStatus.Missed);
                    break;
                case "cancelled":
                    query = query.Where(f => f.Status == FollowUpStatus.Cancelled);
                    break;
                case "planned":
                    query = query.Where(f => f.Status == FollowUpStatus.Planned);
                    break;
                default:
                    break;
            }

            // Secondary filters
            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(f => f.FollowUpType == type);
            }

            if (!string.IsNullOrWhiteSpace(assignedTo))
            {
                query = query.Where(f => f.AssignedTo == assignedTo);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(f =>
                    (f.Remarks != null && f.Remarks.ToLower().Contains(term)) ||
                    (f.Customer != null && f.Customer.CustomerName.ToLower().Contains(term)) ||
                    (f.Lead != null && f.Lead.LeadName.ToLower().Contains(term)) ||
                    f.FollowUpType.ToLower().Contains(term));
            }

            int filteredCount = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(filteredCount / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var items = await query
                .OrderBy(f => f.FollowUpDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var users = await _context.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.FullName)
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = $"{u.FullName} ({u.Email})"
                })
                .ToListAsync();

            var typeList = FollowUpType.All.Select(t => new SelectListItem { Value = t, Text = t }).ToList();
            var statusList = FollowUpStatus.All.Select(s => new SelectListItem { Value = s, Text = s }).ToList();

            return new FollowUpListViewModel
            {
                FollowUps = items,
                Filter = filter,
                TypeFilter = type,
                AssignedToFilter = assignedTo,
                SearchTerm = search,
                TotalCount = totalCount,
                TodayCount = todayCount,
                UpcomingCount = upcomingCount,
                OverdueCount = overdueCount,
                CompletedCount = completedCount,
                PageNumber = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                UsersList = users,
                TypesList = typeList,
                StatusesList = statusList
            };
        }

        public async Task<FollowUp?> GetFollowUpByIdAsync(int id, string currentUserId, string role)
        {
            var followUp = await _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.AssignedToUser)
                .FirstOrDefaultAsync(f => f.FollowUpId == id);

            if (followUp == null) return null;

            if (role == AppRoles.SalesExecutive && followUp.AssignedTo != currentUserId)
            {
                return null;
            }

            return followUp;
        }

        public async Task<FollowUpFormViewModel> GetCreateViewModelAsync(int? customerId = null, int? leadId = null, string? currentUserId = null)
        {
            var model = new FollowUpFormViewModel
            {
                CustomerId = customerId,
                LeadId = leadId,
                FollowUpDate = DateTime.UtcNow.AddDays(1).Date.AddHours(11), // 11:00 AM next day
                AssignedTo = currentUserId
            };

            await PopulateDropdownsAsync(model);
            return model;
        }

        public async Task<FollowUpFormViewModel?> GetEditViewModelAsync(int id, string currentUserId, string role)
        {
            var followUp = await GetFollowUpByIdAsync(id, currentUserId, role);
            if (followUp == null) return null;

            var model = new FollowUpFormViewModel
            {
                FollowUpId = followUp.FollowUpId,
                CustomerId = followUp.CustomerId,
                LeadId = followUp.LeadId,
                FollowUpDate = followUp.FollowUpDate,
                FollowUpType = followUp.FollowUpType,
                Remarks = followUp.Remarks,
                Status = followUp.Status,
                AssignedTo = followUp.AssignedTo
            };

            await PopulateDropdownsAsync(model);
            return model;
        }

        public async Task<FollowUp> CreateFollowUpAsync(FollowUpFormViewModel model, string currentUserId, string ipAddress)
        {
            var followUp = new FollowUp
            {
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                FollowUpDate = model.FollowUpDate,
                FollowUpType = model.FollowUpType,
                Remarks = AcxiomCRM.Common.SanitizationHelper.SanitizeText(model.Remarks),
                Status = FollowUpStatus.Planned,
                AssignedTo = model.AssignedTo ?? currentUserId,
                CreatedDate = DateTime.UtcNow
            };

            _context.FollowUps.Add(followUp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                AuditAction.Create,
                nameof(FollowUp),
                followUp.FollowUpId.ToString(),
                null,
                followUp,
                ipAddress);

            return followUp;
        }

        public async Task<bool> UpdateFollowUpAsync(FollowUpFormViewModel model, string currentUserId, string ipAddress)
        {
            var followUp = await _context.FollowUps.FindAsync(model.FollowUpId);
            if (followUp == null) return false;

            var oldSnapshot = new
            {
                followUp.FollowUpDate,
                followUp.FollowUpType,
                followUp.Remarks,
                followUp.Status,
                followUp.AssignedTo
            };

            followUp.CustomerId = model.CustomerId;
            followUp.LeadId = model.LeadId;
            followUp.FollowUpDate = model.FollowUpDate;
            followUp.FollowUpType = model.FollowUpType;
            followUp.Remarks = AcxiomCRM.Common.SanitizationHelper.SanitizeText(model.Remarks);
            followUp.Status = model.Status;
            followUp.AssignedTo = model.AssignedTo;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                AuditAction.Update,
                nameof(FollowUp),
                followUp.FollowUpId.ToString(),
                oldSnapshot,
                followUp,
                ipAddress);

            return true;
        }

        public async Task<bool> CompleteFollowUpAsync(int id, string? remarks, string currentUserId, string ipAddress)
        {
            var followUp = await _context.FollowUps.FindAsync(id);
            if (followUp == null) return false;

            var oldStatus = followUp.Status;
            followUp.Status = FollowUpStatus.Completed;

            if (!string.IsNullOrWhiteSpace(remarks))
            {
                var completionNote = $"[Completed on {DateTime.UtcNow:dd-MMM-yyyy HH:mm} UTC]: {remarks}";
                followUp.Remarks = string.IsNullOrWhiteSpace(followUp.Remarks)
                    ? completionNote
                    : $"{followUp.Remarks}\n{completionNote}";
            }

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                AuditAction.Update,
                nameof(FollowUp),
                followUp.FollowUpId.ToString(),
                new { Status = oldStatus },
                new { Status = followUp.Status, Remarks = followUp.Remarks },
                ipAddress);

            return true;
        }

        public async Task<bool> RescheduleFollowUpAsync(int id, DateTime newDate, string? remarks, string currentUserId, string ipAddress)
        {
            var followUp = await _context.FollowUps.FindAsync(id);
            if (followUp == null) return false;

            var oldDate = followUp.FollowUpDate;
            followUp.FollowUpDate = newDate;
            followUp.Status = FollowUpStatus.Planned;

            var rescheduleNote = $"[Rescheduled on {DateTime.UtcNow:dd-MMM-yyyy} from {oldDate:dd-MMM-yyyy HH:mm} to {newDate:dd-MMM-yyyy HH:mm}]";
            if (!string.IsNullOrWhiteSpace(remarks))
            {
                rescheduleNote += $": {remarks}";
            }

            followUp.Remarks = string.IsNullOrWhiteSpace(followUp.Remarks)
                ? rescheduleNote
                : $"{followUp.Remarks}\n{rescheduleNote}";

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                AuditAction.Update,
                nameof(FollowUp),
                followUp.FollowUpId.ToString(),
                new { FollowUpDate = oldDate },
                new { FollowUpDate = followUp.FollowUpDate, Status = followUp.Status },
                ipAddress);

            return true;
        }

        public async Task<bool> CancelFollowUpAsync(int id, string? remarks, string currentUserId, string ipAddress)
        {
            var followUp = await _context.FollowUps.FindAsync(id);
            if (followUp == null) return false;

            var oldStatus = followUp.Status;
            followUp.Status = FollowUpStatus.Cancelled;

            if (!string.IsNullOrWhiteSpace(remarks))
            {
                var cancelNote = $"[Cancelled on {DateTime.UtcNow:dd-MMM-yyyy HH:mm} UTC]: {remarks}";
                followUp.Remarks = string.IsNullOrWhiteSpace(followUp.Remarks)
                    ? cancelNote
                    : $"{followUp.Remarks}\n{cancelNote}";
            }

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                AuditAction.Update,
                nameof(FollowUp),
                followUp.FollowUpId.ToString(),
                new { Status = oldStatus },
                new { Status = followUp.Status, Remarks = followUp.Remarks },
                ipAddress);

            return true;
        }

        public async Task AutoMarkOverdueAsync()
        {
            var now = DateTime.UtcNow;
            var overduePlanned = await _context.FollowUps
                .Where(f => f.Status == FollowUpStatus.Planned && f.FollowUpDate < now)
                .ToListAsync();

            if (overduePlanned.Any())
            {
                foreach (var f in overduePlanned)
                {
                    f.Status = FollowUpStatus.Missed;
                }
                await _context.SaveChangesAsync();
            }
        }

        private async Task PopulateDropdownsAsync(FollowUpFormViewModel model)
        {
            model.CustomersList = await _context.Customers
                .Where(c => c.Status == CustomerStatus.Active)
                .OrderBy(c => c.CustomerName)
                .Select(c => new SelectListItem
                {
                    Value = c.CustomerId.ToString(),
                    Text = $"{c.CustomerName} ({c.CustomerCode}) - {c.CompanyName ?? c.City}"
                })
                .ToListAsync();

            model.LeadsList = await _context.Leads
                .Where(l => l.Status != LeadStatus.Converted && l.Status != LeadStatus.Lost)
                .OrderBy(l => l.LeadName)
                .Select(l => new SelectListItem
                {
                    Value = l.LeadId.ToString(),
                    Text = $"{l.LeadName} ({l.LeadCode}) - {l.CompanyName ?? l.Source}"
                })
                .ToListAsync();

            model.UsersList = await _context.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.FullName)
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = $"{u.FullName} ({u.Email})"
                })
                .ToListAsync();

            model.TypesList = FollowUpType.All.Select(t => new SelectListItem
            {
                Value = t,
                Text = t
            }).ToList();
        }
    }
}

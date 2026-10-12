using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Models.Constants;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Services
{
    public class ActivityService : IActivityService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserScopeService _userScopeService;
        private readonly IAuditService _auditService;

        public ActivityService(
            ApplicationDbContext context,
            IUserScopeService userScopeService,
            IAuditService auditService)
        {
            _context = context;
            _userScopeService = userScopeService;
            _auditService = auditService;
        }

        public async Task<ActivityListViewModel> GetActivitiesAsync(
            string currentUserId,
            string role,
            string? type,
            string? status,
            string? assignedTo,
            string? search,
            string viewMode = "table",
            int page = 1,
            int pageSize = 10)
        {
            var query = _context.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .Include(a => a.AssignedToUser)
                .AsQueryable();

            if (role == AppRoles.SalesExecutive)
            {
                query = query.Where(a => a.AssignedTo == currentUserId);
            }

            // Aggregated KPI counts
            var scopedBase = query;
            int totalActivitiesCount = await scopedBase.CountAsync();
            int pendingTasksCount = await scopedBase.CountAsync(a => a.Status == ActivityStatus.Pending);
            int callsCount = await scopedBase.CountAsync(a => a.ActivityType == ActivityType.Call);
            int meetingsCount = await scopedBase.CountAsync(a => a.ActivityType == ActivityType.Meeting);
            int completedActivitiesCount = await scopedBase.CountAsync(a => a.Status == ActivityStatus.Completed);

            // Filtering
            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(a => a.ActivityType == type);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(a => a.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(assignedTo))
            {
                query = query.Where(a => a.AssignedTo == assignedTo);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(a =>
                    a.Subject.ToLower().Contains(term) ||
                    (a.Description != null && a.Description.ToLower().Contains(term)) ||
                    (a.Customer != null && a.Customer.CustomerName.ToLower().Contains(term)) ||
                    (a.Lead != null && a.Lead.LeadName.ToLower().Contains(term)));
            }

            int filteredCount = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(filteredCount / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var items = await query
                .OrderByDescending(a => a.ActivityDate)
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

            var typeList = ActivityType.All.Select(t => new SelectListItem { Value = t, Text = t }).ToList();
            var statusList = ActivityStatus.All.Select(s => new SelectListItem { Value = s, Text = s }).ToList();

            return new ActivityListViewModel
            {
                Activities = items,
                TypeFilter = type,
                StatusFilter = status,
                AssignedToFilter = assignedTo,
                SearchTerm = search,
                ViewMode = viewMode,
                TotalActivitiesCount = totalActivitiesCount,
                PendingTasksCount = pendingTasksCount,
                CallsCount = callsCount,
                MeetingsCount = meetingsCount,
                CompletedActivitiesCount = completedActivitiesCount,
                PageNumber = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                UsersList = users,
                TypesList = typeList,
                StatusesList = statusList
            };
        }

        public async Task<List<Activity>> GetTimelineAsync(
            string currentUserId,
            string role,
            int? customerId = null,
            int? leadId = null,
            int take = 50)
        {
            var query = _context.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .Include(a => a.AssignedToUser)
                .AsQueryable();

            if (role == AppRoles.SalesExecutive)
            {
                query = query.Where(a => a.AssignedTo == currentUserId);
            }

            if (customerId.HasValue)
            {
                query = query.Where(a => a.CustomerId == customerId.Value);
            }

            if (leadId.HasValue)
            {
                query = query.Where(a => a.LeadId == leadId.Value);
            }

            return await query
                .OrderByDescending(a => a.ActivityDate)
                .Take(take)
                .ToListAsync();
        }

        public async Task<Activity?> GetActivityByIdAsync(int id, string currentUserId, string role)
        {
            var activity = await _context.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .Include(a => a.AssignedToUser)
                .FirstOrDefaultAsync(a => a.ActivityId == id);

            if (activity == null) return null;

            if (role == AppRoles.SalesExecutive && activity.AssignedTo != currentUserId)
            {
                return null;
            }

            return activity;
        }

        public async Task<ActivityFormViewModel> GetCreateViewModelAsync(int? customerId = null, int? leadId = null, string? currentUserId = null)
        {
            var model = new ActivityFormViewModel
            {
                CustomerId = customerId,
                LeadId = leadId,
                ActivityDate = DateTime.UtcNow,
                AssignedTo = currentUserId
            };

            await PopulateDropdownsAsync(model);
            return model;
        }

        public async Task<ActivityFormViewModel?> GetEditViewModelAsync(int id, string currentUserId, string role)
        {
            var activity = await GetActivityByIdAsync(id, currentUserId, role);
            if (activity == null) return null;

            var model = new ActivityFormViewModel
            {
                ActivityId = activity.ActivityId,
                ActivityType = activity.ActivityType,
                Subject = activity.Subject,
                Description = activity.Description,
                ActivityDate = activity.ActivityDate,
                CustomerId = activity.CustomerId,
                LeadId = activity.LeadId,
                Status = activity.Status,
                AssignedTo = activity.AssignedTo
            };

            await PopulateDropdownsAsync(model);
            return model;
        }

        public async Task<Activity> CreateActivityAsync(ActivityFormViewModel model, string currentUserId, string ipAddress)
        {
            var activity = new Activity
            {
                ActivityType = model.ActivityType,
                Subject = AcxiomCRM.Common.SanitizationHelper.SanitizeText(model.Subject) ?? string.Empty,
                Description = AcxiomCRM.Common.SanitizationHelper.SanitizeText(model.Description),
                ActivityDate = model.ActivityDate,
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                Status = model.Status,
                AssignedTo = model.AssignedTo ?? currentUserId,
                CreatedDate = DateTime.UtcNow
            };

            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                AuditAction.Create,
                nameof(Activity),
                activity.ActivityId.ToString(),
                null,
                activity,
                ipAddress);

            return activity;
        }

        public async Task<bool> UpdateActivityAsync(ActivityFormViewModel model, string currentUserId, string ipAddress)
        {
            var activity = await _context.Activities.FindAsync(model.ActivityId);
            if (activity == null) return false;

            var oldSnapshot = new
            {
                activity.Subject,
                activity.ActivityType,
                activity.Description,
                activity.ActivityDate,
                activity.Status,
                activity.AssignedTo
            };

            activity.Subject = AcxiomCRM.Common.SanitizationHelper.SanitizeText(model.Subject) ?? string.Empty;
            activity.ActivityType = model.ActivityType;
            activity.Description = AcxiomCRM.Common.SanitizationHelper.SanitizeText(model.Description);
            activity.ActivityDate = model.ActivityDate;
            activity.CustomerId = model.CustomerId;
            activity.LeadId = model.LeadId;
            activity.Status = model.Status;
            activity.AssignedTo = model.AssignedTo;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                AuditAction.Update,
                nameof(Activity),
                activity.ActivityId.ToString(),
                oldSnapshot,
                activity,
                ipAddress);

            return true;
        }

        public async Task<bool> CompleteActivityAsync(int id, string? completionNote, string currentUserId, string ipAddress)
        {
            var activity = await _context.Activities.FindAsync(id);
            if (activity == null) return false;

            var oldStatus = activity.Status;
            activity.Status = ActivityStatus.Completed;

            if (!string.IsNullOrWhiteSpace(completionNote))
            {
                var note = $"[Completed on {DateTime.UtcNow:dd-MMM-yyyy HH:mm} UTC]: {completionNote}";
                activity.Description = string.IsNullOrWhiteSpace(activity.Description)
                    ? note
                    : $"{activity.Description}\n{note}";
            }

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                AuditAction.Update,
                nameof(Activity),
                activity.ActivityId.ToString(),
                new { Status = oldStatus },
                new { Status = activity.Status, Description = activity.Description },
                ipAddress);

            return true;
        }

        private async Task PopulateDropdownsAsync(ActivityFormViewModel model)
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

            model.TypesList = ActivityType.All.Select(t => new SelectListItem
            {
                Value = t,
                Text = t
            }).ToList();

            model.StatusesList = ActivityStatus.All.Select(s => new SelectListItem
            {
                Value = s,
                Text = s
            }).ToList();
        }
    }
}

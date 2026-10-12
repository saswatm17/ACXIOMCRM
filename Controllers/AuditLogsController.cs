using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Constants;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers
{
    [Authorize(Roles = AppRoles.Admin)]
    public class AuditLogsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditLogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            string? actionType,
            string? entityName,
            string? userId,
            DateTime? fromDate,
            DateTime? toDate,
            string? search,
            int page = 1)
        {
            var query = _context.AuditLogs
                .Include(a => a.User)
                .AsQueryable();

            // Aggregated counts for KPI cards
            var baseQuery = _context.AuditLogs.AsQueryable();
            int totalLogsCount = await baseQuery.CountAsync();
            int createsCount = await baseQuery.CountAsync(a => a.Action == AuditAction.Create);
            int updatesCount = await baseQuery.CountAsync(a => a.Action == AuditAction.Update);
            int securityEventsCount = await baseQuery.CountAsync(a =>
                a.Action == AuditAction.Login ||
                a.Action == AuditAction.FailedLogin ||
                a.Action == AuditAction.Logout ||
                a.Action == AuditAction.PasswordChange ||
                a.Action == AuditAction.RoleChange);

            // Filtering
            if (!string.IsNullOrWhiteSpace(actionType))
            {
                query = query.Where(a => a.Action == actionType);
            }

            if (!string.IsNullOrWhiteSpace(entityName))
            {
                query = query.Where(a => a.EntityName == entityName);
            }

            if (!string.IsNullOrWhiteSpace(userId))
            {
                query = query.Where(a => a.UserId == userId);
            }

            if (fromDate.HasValue)
            {
                var start = fromDate.Value.Date;
                query = query.Where(a => a.CreatedDate >= start);
            }

            if (toDate.HasValue)
            {
                var end = toDate.Value.Date.AddDays(1);
                query = query.Where(a => a.CreatedDate < end);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(a =>
                    (a.RecordId != null && a.RecordId.ToLower().Contains(term)) ||
                    (a.IpAddress != null && a.IpAddress.ToLower().Contains(term)) ||
                    (a.OldValue != null && a.OldValue.ToLower().Contains(term)) ||
                    (a.NewValue != null && a.NewValue.ToLower().Contains(term)) ||
                    (a.User != null && a.User.FullName.ToLower().Contains(term)) ||
                    (a.User != null && a.User.Email != null && a.User.Email.ToLower().Contains(term)));
            }

            int filteredCount = await query.CountAsync();
            int pageSize = 15;
            int totalPages = (int)Math.Ceiling(filteredCount / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var items = await query
                .OrderByDescending(a => a.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Distinct lists for dropdowns
            var actionList = new List<string>
            {
                AuditAction.Login,
                AuditAction.FailedLogin,
                AuditAction.Logout,
                AuditAction.Create,
                AuditAction.Update,
                AuditAction.Delete,
                AuditAction.RoleChange,
                AuditAction.PasswordChange,
                AuditAction.LeadConversion
            }.Select(a => new SelectListItem { Value = a, Text = a }).ToList();

            var entityList = await _context.AuditLogs
                .Select(a => a.EntityName)
                .Distinct()
                .OrderBy(e => e)
                .Select(e => new SelectListItem { Value = e, Text = e })
                .ToListAsync();

            var usersList = await _context.Users
                .OrderBy(u => u.FullName)
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = $"{u.FullName} ({u.Email})"
                })
                .ToListAsync();

            var viewModel = new AuditLogListViewModel
            {
                AuditLogs = items,
                ActionFilter = actionType,
                EntityFilter = entityName,
                UserFilter = userId,
                FromDate = fromDate,
                ToDate = toDate,
                SearchTerm = search,
                TotalLogsCount = totalLogsCount,
                CreatesCount = createsCount,
                UpdatesCount = updatesCount,
                SecurityEventsCount = securityEventsCount,
                PageNumber = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalItems = filteredCount,
                ActionsList = actionList,
                EntitiesList = entityList,
                UsersList = usersList
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Details(int id)
        {
            var log = await _context.AuditLogs
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.AuditLogId == id);

            if (log == null)
            {
                return NotFound();
            }

            var model = new AuditLogDetailsViewModel
            {
                Log = log
            };

            // Parse and format JSON diffs
            var serializerOptions = new JsonSerializerOptions { WriteIndented = true };

            var oldDict = new Dictionary<string, string>();
            var newDict = new Dictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(log.OldValue))
            {
                try
                {
                    using var doc = JsonDocument.Parse(log.OldValue);
                    model.FormattedOldJson = JsonSerializer.Serialize(doc.RootElement, serializerOptions);

                    if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var prop in doc.RootElement.EnumerateObject())
                        {
                            oldDict[prop.Name] = prop.Value.ToString();
                        }
                    }
                }
                catch
                {
                    model.FormattedOldJson = log.OldValue;
                }
            }

            if (!string.IsNullOrWhiteSpace(log.NewValue))
            {
                try
                {
                    using var doc = JsonDocument.Parse(log.NewValue);
                    model.FormattedNewJson = JsonSerializer.Serialize(doc.RootElement, serializerOptions);

                    if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var prop in doc.RootElement.EnumerateObject())
                        {
                            newDict[prop.Name] = prop.Value.ToString();
                        }
                    }
                }
                catch
                {
                    model.FormattedNewJson = log.NewValue;
                }
            }

            // Build property comparison diff
            var allKeys = oldDict.Keys.Union(newDict.Keys).OrderBy(k => k).ToList();
            foreach (var key in allKeys)
            {
                oldDict.TryGetValue(key, out var oldVal);
                newDict.TryGetValue(key, out var newVal);

                model.PropertyDiffs.Add(new PropertyDiffItem
                {
                    PropertyName = key,
                    OldValue = oldVal ?? "(none)",
                    NewValue = newVal ?? "(none)",
                    IsChanged = oldVal != newVal
                });
            }

            return View(model);
        }
    }
}

using System.Text.Json;
using AcxiomCRM.Data;
using AcxiomCRM.Models;

namespace AcxiomCRM.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;

        public AuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(
            string? userId,
            string action,
            string entityName,
            string? recordId,
            object? oldValue,
            object? newValue,
            string? ipAddress)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                var log = new AuditLog
                {
                    UserId = userId,
                    Action = action,
                    EntityName = entityName,
                    RecordId = recordId,
                    OldValue = oldValue != null ? JsonSerializer.Serialize(oldValue, options) : null,
                    NewValue = newValue != null ? JsonSerializer.Serialize(newValue, options) : null,
                    CreatedDate = DateTime.UtcNow,
                    IpAddress = ipAddress ?? "127.0.0.1"
                };

                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Audit logging failure should not crash primary business operations
            }
        }
    }
}

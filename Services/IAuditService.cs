namespace AcxiomCRM.Services
{
    public interface IAuditService
    {
        Task LogAsync(string? userId, string action, string entityName, string? recordId, object? oldValue, object? newValue, string? ipAddress);
    }
}

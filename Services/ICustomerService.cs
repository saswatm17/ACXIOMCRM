using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Services
{
    public interface ICustomerService
    {
        Task<CustomerListViewModel> GetCustomersAsync(string? search, string? status, int page, int pageSize, string? currentUserId, bool isElevatedRole);
        Task<Customer?> GetCustomerByIdAsync(int id);
        Task<(bool Success, string? ErrorMessage, Customer? Customer)> CreateCustomerAsync(CustomerFormViewModel model, string userId, string? ipAddress);
        Task<(bool Success, string? ErrorMessage)> UpdateCustomerAsync(CustomerFormViewModel model, string userId, string? ipAddress);
        Task<(bool Success, string? ErrorMessage)> DeactivateCustomerAsync(int id, string userId, string? ipAddress);
        Task<bool> IsEmailUniqueAsync(string email, int? excludeCustomerId = null);
        Task<bool> IsPhoneUniqueAsync(string phone, int? excludeCustomerId = null);
    }
}

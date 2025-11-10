using Customer_Relationship_Management.Models;

namespace Customer_Relationship_Management.Services.Interfaces
{
    public interface ICustomerService
    {
        Task<IEnumerable<Customer>> GetCustomersForUserAsync(int userId);
        Task<Customer?> GetCustomerByCustomerID_UserIDAsync(int customerId, int userId);
        Task<(bool Success, string Message)> AddCustomerAsync(Customer customer, int userId);

        Task<bool> UpdateCustomerAsync(Customer customer, int userId);
        Task<bool> DeleteCustomerAsync(int customerId, int userId);
        Task<Customer?> GetCustomerByCustomerID(int customerId);


        // ⭐ Manager actions
        Task<IEnumerable<Customer>> GetCustomersForTeamAsync(IEnumerable<int> teamUserIds);
        Task<(bool Success, string Message)> ReassignCustomerAsync(int customerId, int newUserId, int managerId);
        Task<(bool Success, string Message)> ToggleVIPAsync(int customerId, int managerId);

    }
}

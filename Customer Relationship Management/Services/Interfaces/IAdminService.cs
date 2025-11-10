using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.ViewModels.User;

namespace Customer_Relationship_Management.Services.Interfaces
{
    public interface IAdminService
    {
        // Dashboard metrics
        Task<AdminViewModel> GetDashboardMetricsAsync();

        // User management
        Task<IEnumerable<AdminUserControlViewModel>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(int id);
        Task<(bool Success, string Message)> CreateUserAsync(User user, string plainPassword, int adminId);
        Task<(bool Success, string Message)> UpdateUserAsync(User user, int adminId);
        Task<(bool Success, string Message)> ToggleUserLockAsync(int id, int adminId);
        Task<(bool Success, string Message)> ForceChangePasswordAsync(int id, int adminId);
        Task<(bool Success, string Message)> ChangeUserRoleAsync(int id, int roleId, int adminId);

        // Audit logs
        Task<IEnumerable<Customer_Relationship_Management.Models.AuditLog>> GetAuditLogsAsync(string? tableName = null, int? userId = null, Customer_Relationship_Management.Models.ActionType? action = null);
    }
}
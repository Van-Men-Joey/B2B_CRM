using Customer_Relationship_Management.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Customer_Relationship_Management.Services.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<User>> GetAllAsync();
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByRoleIDAsync(int roleID);
        Task<User?> GetByUserCodeAsync(string userCode);
        Task<User?> GetByFullNameAsync(string fullName);
        Task<User?> GetByPhoneAsync(string phone);

        // Add / Update / Delete with optional currentUserId for auditing
        Task AddUserAsync(User user, int? currentUserId = null);
        Task UpdateUserAsync(User user, int? currentUserId = null);
        Task DeleteUserAsync(int id, int? currentUserId = null);
        /// <summary>
        /// Lấy danh sách nhân viên trong team theo ManagerID
        /// </summary>
        Task<IEnumerable<User>> GetEmployeesByManagerAsync(int managerId);
    }
}
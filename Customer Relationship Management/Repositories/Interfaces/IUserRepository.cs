using Customer_Relationship_Management.Models;

namespace Customer_Relationship_Management.Repositories.Interfaces
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByRoleIDAsync(int roleID);
        Task<User?> GetByUserCodeAsync(string userCode);
        Task<User?> GetByFullNameAsync(string fullName);
        Task<User?> GetByPhoneAsync(string phone);
        Task<IEnumerable<User>> GetAllWithRolesAsync();
        Task<IEnumerable<User>> GetEmployeesByManagerAsync(int managerId);
    }
}

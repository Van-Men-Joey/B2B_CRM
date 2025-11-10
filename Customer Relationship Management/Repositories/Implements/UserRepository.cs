using Customer_Relationship_Management.Data;
using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Customer_Relationship_Management.Repositories.Implements
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(B2BDbContext context) : base(context) { }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByRoleIDAsync(int roleID)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.RoleID == roleID);
        }

        public async Task<User?> GetByUserCodeAsync(string userCode)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserCode == userCode);
        }

        public async Task<User?> GetByFullNameAsync(string fullName)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.FullName == fullName);
        }

        public async Task<User?> GetByPhoneAsync(string phone)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Phone == phone);
        }

        public async Task<IEnumerable<User>> GetAllWithRolesAsync()
        {
            return await _context.Users
                .Include(u => u.Role)
                .ToListAsync();
        }
        /// <summary>
        /// Lấy danh sách nhân viên trong team theo ManagerID
        /// </summary>
        public async Task<IEnumerable<User>> GetEmployeesByManagerAsync(int managerId)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Where(u => u.ManagerID == managerId && !u.IsDeleted)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}

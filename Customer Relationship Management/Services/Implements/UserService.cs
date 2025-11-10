using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Repositories.Interfaces;
using Customer_Relationship_Management.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Customer_Relationship_Management.Services.Implements
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IAuditLogService _auditLogService;

        public UserService(IUserRepository userRepository, IAuditLogService auditLogService)
        {
            _userRepository = userRepository;
            _auditLogService = auditLogService;
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _userRepository.GetAllWithRolesAsync();
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _userRepository.GetByIdAsync(id);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _userRepository.GetByEmailAsync(email);
        }

        public async Task<User?> GetByRoleIDAsync(int roleID)
        {
            return await _userRepository.GetByRoleIDAsync(roleID);
        }

        public async Task<User?> GetByUserCodeAsync(string userCode)
        {
            return await _userRepository.GetByUserCodeAsync(userCode);
        }

        public async Task<User?> GetByFullNameAsync(string fullName)
        {
            return await _userRepository.GetByFullNameAsync(fullName);
        }

        public async Task<User?> GetByPhoneAsync(string phone)
        {
            return await _userRepository.GetByPhoneAsync(phone);
        }

        // Create user with optional currentUserId (who performs the action) -> Audit logged
        public async Task AddUserAsync(User user, int? currentUserId = null)
        {
            // Business rule: Employee must have a Manager
            if (user.RoleID == 1 && user.ManagerID == null)
            {
                throw new ArgumentException("Nhân viên (Employee) bắt buộc phải có ManagerID.");
            }

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            // ensure user.UserID has been assigned by DB
            try
            {
                await _auditLogService.LogAsync(
                    userId: currentUserId,
                    action: ActionType.Create,
                    tableName: "Users",
                    recordId: user.UserID.ToString(),
                    oldValue: null,
                    newValue: user
                );
            }
            catch
            {
                // don't break main flow if audit fails; but consider logging the exception somewhere
            }
        }

        public async Task UpdateUserAsync(User user, int? currentUserId = null)
        {
            var existing = await _userRepository.GetByIdAsync(user.UserID);
            if (existing == null) return;

            // Business rule: Employee must have a Manager
            var roleId = user.RoleID != 0 ? user.RoleID : existing.RoleID;
            var managerId = user.ManagerID ?? existing.ManagerID;
            if (roleId == 1 && managerId == null)
            {
                throw new ArgumentException("Nhân viên (Employee) bắt buộc phải có ManagerID.");
            }

            // Snapshot old
            object? oldSnapshot = null;
            try
            {
                var oldJson = JsonSerializer.Serialize(existing);
                oldSnapshot = JsonSerializer.Deserialize<object>(oldJson);
            }
            catch
            {
                // ignore snapshot failure
            }

            // Apply updates
            existing.FullName = user.FullName;
            existing.Email = user.Email;
            existing.Phone = user.Phone;
            existing.Status = user.Status;
            existing.ManagerID = user.ManagerID ?? existing.ManagerID;
            existing.RoleID = roleId;
            existing.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(existing);
            await _userRepository.SaveChangesAsync();

            // Snapshot new
            object? newSnapshot = null;
            try
            {
                var newJson = JsonSerializer.Serialize(existing);
                newSnapshot = JsonSerializer.Deserialize<object>(newJson);
            }
            catch
            {
                // ignore
            }

            // Log update
            try
            {
                await _auditLogService.LogAsync(
                    userId: currentUserId,
                    action: ActionType.Update,
                    tableName: "Users",
                    recordId: existing.UserID.ToString(),
                    oldValue: oldSnapshot,
                    newValue: newSnapshot
                );
            }
            catch
            {
                // ignore audit failures
            }
        }

        public async Task DeleteUserAsync(int id, int? currentUserId = null)
        {
            var existing = await _userRepository.GetByIdAsync(id);
            if (existing != null)
            {
                // Snapshot old
                object? oldSnapshot = null;
                try
                {
                    var oldJson = JsonSerializer.Serialize(existing);
                    oldSnapshot = JsonSerializer.Deserialize<object>(oldJson);
                }
                catch
                {
                    // ignore
                }

                await _userRepository.DeleteAsync(existing);
                await _userRepository.SaveChangesAsync();

                try
                {
                    await _auditLogService.LogAsync(
                        userId: currentUserId,
                        action: ActionType.Delete,
                        tableName: "Users",
                        recordId: existing.UserID.ToString(),
                        oldValue: oldSnapshot,
                        newValue: null
                    );
                }
                catch
                {
                    // ignore
                }
            }
        }

        /// <summary>
        /// Lấy danh sách nhân viên trong team theo ManagerID
        /// </summary>
        public async Task<IEnumerable<User>> GetEmployeesByManagerAsync(int managerId)
            => await _userRepository.GetEmployeesByManagerAsync(managerId);
    }

}
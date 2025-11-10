using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Repositories.Interfaces;
using Customer_Relationship_Management.Services.Interfaces;
using Customer_Relationship_Management.ViewModels.User;
using Customer_Relationship_Management.Security;
using Microsoft.Extensions.Logging;

namespace Customer_Relationship_Management.Services.Implements
{
    public class AdminService : IAdminService
    {
        private readonly IUserRepository _userRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IDealRepository _dealRepository;
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IGenericRepository<Role> _roleRepo;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<AdminService> _logger;

        public AdminService(
            IUserRepository userRepository,
            ICustomerRepository customerRepository,
            IDealRepository dealRepository,
            IAuditLogRepository auditLogRepository,
            IGenericRepository<Role> roleRepo,
            IAuditLogService auditLogService,
            ILogger<AdminService> logger)
        {
            _userRepository = userRepository;
            _customerRepository = customerRepository;
            _dealRepository = dealRepository;
            _auditLogRepository = auditLogRepository;
            _roleRepo = roleRepo;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        public async Task<AdminViewModel> GetDashboardMetricsAsync()
        {
            var users = await _userRepository.GetAllWithRolesAsync();
            var customers = await _customerRepository.GetAllAsync();
            var deals = await _dealRepository.GetAllAsync();

            var vm = new AdminViewModel
            {
                TotalUsers = users.Count(),
                TotalCustomers = (int)customers.Count(),
                TotalDeals = (int)deals.Count(),
                TotalSupportTickets = 0 // extend when you have ticket repo
            };

            return vm;
        }

        public async Task<IEnumerable<AdminUserControlViewModel>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllWithRolesAsync();
            return users.Select(u => new AdminUserControlViewModel
            {
                UserID = u.UserID,
                UserCode = u.UserCode ?? string.Empty,
                Username = u.Username,
                FullName = u.FullName,
                Email = u.Email,
                RoleName = u.Role?.RoleName ?? string.Empty,
                IsDeleted = u.IsDeleted,
                ForceChangePassword = u.ForceChangePassword,
                TwoFAEnabled = u.TwoFAEnabled,
                Status = u.Status
            });
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _userRepository.GetByIdAsync(id);
        }

        public async Task<(bool Success, string Message)> CreateUserAsync(User user, string plainPassword, int adminId)
        {
            if (string.IsNullOrWhiteSpace(plainPassword))
                return (false, "Password is required.");

            // basic validation
            if (string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.Email) || string.IsNullOrWhiteSpace(user.FullName))
                return (false, "Required fields missing.");

            // hash password
            user.PasswordHash = PasswordHasher.HashPassword(plainPassword);
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            user.IsDeleted = false;

            try
            {
                await _userRepository.AddAsync(user);
                await _userRepository.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    userId: adminId,
                    action: ActionType.Create,
                    tableName: "Users",
                    recordId: user.UserID.ToString(),
                    oldValue: null,
                    newValue: user
                );

                return (true, "User created.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string Message)> UpdateUserAsync(User user, int adminId)
        {
            var existing = await _userRepository.GetByIdAsync(user.UserID);
            if (existing == null) return (false, "User not found.");

            var oldJson = Customer_Relationship_Management.Models.AuditLog.ToJson(existing);

            existing.FullName = user.FullName;
            existing.Email = user.Email;
            existing.Phone = user.Phone;
            existing.Status = user.Status;
            existing.ForceChangePassword = user.ForceChangePassword;
            existing.TwoFAEnabled = user.TwoFAEnabled;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.RoleID = user.RoleID;

            await _userRepository.UpdateAsync(existing);
            await _userRepository.SaveChangesAsync();

            await _auditLogService.LogAsync(
                userId: adminId,
                action: ActionType.Update,
                tableName: "Users",
                recordId: existing.UserID.ToString(),
                oldValue: oldJson,
                newValue: existing
            );

            return (true, "User updated.");
        }

        public async Task<(bool Success, string Message)> ToggleUserLockAsync(int id, int adminId)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return (false, "User not found.");

            user.IsDeleted = !user.IsDeleted;
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            await _auditLogService.LogAsync(
                userId: adminId,
                action: ActionType.Update,
                tableName: "Users",
                recordId: user.UserID.ToString(),
                oldValue: null,
                newValue: new { user.IsDeleted }
            );

            return (true, user.IsDeleted ? "User locked." : "User unlocked.");
        }

        public async Task<(bool Success, string Message)> ForceChangePasswordAsync(int id, int adminId)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return (false, "User not found.");

            user.ForceChangePassword = true;
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            await _auditLogService.LogAsync(
                userId: adminId,
                action: ActionType.Update,
                tableName: "Users",
                recordId: user.UserID.ToString(),
                oldValue: null,
                newValue: new { user.ForceChangePassword }
            );

            return (true, "Force change password flag set.");
        }

        public async Task<(bool Success, string Message)> ChangeUserRoleAsync(int id, int roleId, int adminId)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return (false, "User not found.");

            var role = await _roleRepo.GetByIdAsync(roleId);
            if (role == null) return (false, "Role not found.");

            user.RoleID = roleId;
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            await _auditLogService.LogAsync(
                userId: adminId,
                action: ActionType.Update,
                tableName: "Users",
                recordId: user.UserID.ToString(),
                oldValue: null,
                newValue: new { user.RoleID }
            );

            return (true, "Role changed.");
        }

        public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(string? tableName = null, int? userId = null, ActionType? action = null)
        {
            return await _auditLogRepository.FindAsync(tableName, userId, action);
        }
    }
}
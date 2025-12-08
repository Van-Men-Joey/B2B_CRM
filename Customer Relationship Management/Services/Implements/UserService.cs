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

        // Định nghĩa hằng số cho RoleID để code dễ đọc hơn
        private const int ROLE_EMPLOYEE = 1;
        private const int ROLE_MANAGER = 2;
        private const int ROLE_ADMIN = 3;
        private const int ROLE_DIRECTOR = 4;

        public UserService(IUserRepository userRepository, IAuditLogService auditLogService)
        {
            _userRepository = userRepository;
            _auditLogService = auditLogService;
        }

        // ... Các hàm Get (Giữ nguyên) ...
        public async Task<IEnumerable<User>> GetAllAsync() => await _userRepository.GetAllWithRolesAsync();
        public async Task<User?> GetByIdAsync(int id) => await _userRepository.GetByIdAsync(id);
        public async Task<User?> GetByEmailAsync(string email) => await _userRepository.GetByEmailAsync(email);
        public async Task<User?> GetByRoleIDAsync(int roleID) => await _userRepository.GetByRoleIDAsync(roleID);
        public async Task<User?> GetByUserCodeAsync(string userCode) => await _userRepository.GetByUserCodeAsync(userCode);
        public async Task<User?> GetByFullNameAsync(string fullName) => await _userRepository.GetByFullNameAsync(fullName);
        public async Task<User?> GetByPhoneAsync(string phone) => await _userRepository.GetByPhoneAsync(phone);
        public async Task<IEnumerable<User>> GetEmployeesByManagerAsync(int managerId) => await _userRepository.GetEmployeesByManagerAsync(managerId);

        // --- NÂNG CẤP LOGIC ADD ---
        public async Task AddUserAsync(User user, int? currentUserId = null)
        {
            // 1. Bảo vệ Admin: Không cho phép tạo thêm Admin
            if (user.RoleID == ROLE_ADMIN)
            {
                throw new InvalidOperationException("Hệ thống chỉ tồn tại duy nhất một Admin. Không thể tạo thêm.");
            }

            // 2. Logic Manager: Chỉ Employee (Role=1) mới CẦN và ĐƯỢC có Manager
            if (user.RoleID == ROLE_EMPLOYEE)
            {
                if (user.ManagerID == null || user.ManagerID == 0)
                {
                    throw new ArgumentException("Nhân viên (Employee) bắt buộc phải có người quản lý (Manager).");
                }
            }
            else
            {
                // Manager, Director, Admin (nếu lọt qua) thì ManagerID phải là null
                user.ManagerID = null;
            }

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            // Audit Log
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
            catch { /* Ignore audit error */ }
        }

        // --- NÂNG CẤP LOGIC UPDATE ---
        public async Task UpdateUserAsync(User user, int? currentUserId = null)
        {
            var existing = await _userRepository.GetByIdAsync(user.UserID);
            if (existing == null) throw new KeyNotFoundException("User not found");

            // 1. Bảo vệ Admin: Không được sửa Role hoặc Status của Admin gốc
            if (existing.RoleID == ROLE_ADMIN)
            {
                // Nếu user cố tình đổi Role hoặc Status (Lock) của Admin -> Chặn
                if (user.RoleID != ROLE_ADMIN || user.Status != "Active" || user.IsDeleted)
                {
                    throw new InvalidOperationException("Không thể thay đổi quyền hạn hoặc khóa tài khoản Admin quản trị.");
                }
                // Admin chỉ cho phép sửa thông tin cá nhân cơ bản (nếu cần thiết)
            }

            // 2. Logic Manager cho Employee khi update
            var newRole = user.RoleID != 0 ? user.RoleID : existing.RoleID;

            // Nếu chuyển thành Employee (hoặc đang là Employee) -> Check Manager
            if (newRole == ROLE_EMPLOYEE)
            {
                // Nếu input có ManagerID thì lấy, không thì giữ cái cũ
                var mgrId = user.ManagerID ?? existing.ManagerID;
                if (mgrId == null)
                {
                    throw new ArgumentException("Nhân viên (Employee) bắt buộc phải có ManagerID.");
                }
                existing.ManagerID = mgrId;
            }
            else
            {
                // Nếu lên chức (Manager/Director) -> Xóa ManagerID
                existing.ManagerID = null;
            }

            // Snapshot Old
            object? oldSnapshot = null;
            try { oldSnapshot = JsonSerializer.Deserialize<object>(JsonSerializer.Serialize(existing)); } catch { }

            // Apply updates
            existing.FullName = user.FullName;
            existing.Email = user.Email;
            existing.Phone = user.Phone;
            existing.Status = user.Status; // Lưu ý: logic chặn khóa Admin đã xử lý ở trên
            existing.RoleID = newRole;
            existing.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(existing);
            await _userRepository.SaveChangesAsync();

            // Audit Log
            object? newSnapshot = null;
            try { newSnapshot = JsonSerializer.Deserialize<object>(JsonSerializer.Serialize(existing)); } catch { }
            await _auditLogService.LogAsync(currentUserId, ActionType.Update, "Users", existing.UserID.ToString(), oldSnapshot, newSnapshot);
        }

        // --- NÂNG CẤP LOGIC DELETE ---
        public async Task DeleteUserAsync(int id, int? currentUserId = null)
        {
            var existing = await _userRepository.GetByIdAsync(id);
            if (existing == null) return;

            // 1. Bảo vệ Admin
            if (existing.RoleID == ROLE_ADMIN)
            {
                throw new InvalidOperationException("Không thể xóa tài khoản Admin quản trị.");
            }

            object? oldSnapshot = null;
            try { oldSnapshot = JsonSerializer.Deserialize<object>(JsonSerializer.Serialize(existing)); } catch { }

            // Soft Delete
            existing.IsDeleted = true;
            existing.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(existing);
            await _userRepository.SaveChangesAsync();

            await _auditLogService.LogAsync(currentUserId, ActionType.Delete, "Users", existing.UserID.ToString(), oldSnapshot, null);
        }
    }
}
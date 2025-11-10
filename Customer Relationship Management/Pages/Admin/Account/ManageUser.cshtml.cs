using Customer_Relationship_Management.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Security;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace Customer_Relationship_Management.Pages.Admin.Account
{
    [Authorize(Roles = "Admin")]
    public class ManageUserModel : PageModel
    {
        private readonly IUserService _userService;
        public ManageUserModel(IUserService userService)
        {
            _userService = userService;
        }

        public IEnumerable<AdminUserControlViewModel> Users { get; set; } = new List<AdminUserControlViewModel>();
        public List<(int Id, string Display)> Managers { get; set; } = new();

        // Filters (GET)
        [BindProperty(SupportsGet = true)] public int? FilterRoleId { get; set; }
        [BindProperty(SupportsGet = true)] public string? FilterUserCode { get; set; }
        [BindProperty(SupportsGet = true)] public string? FilterUsername { get; set; }
        [BindProperty(SupportsGet = true)] public bool? FilterLocked { get; set; }

        // Create input
        [BindProperty] public CreateUserInput CreateInput { get; set; } = new();

        // Update manager input
        [BindProperty] public UpdateManagerInput UpdateMgr { get; set; } = new();

        // Update role input
        [BindProperty] public UpdateRoleInput UpdateRole { get; set; } = new();

        public async Task OnGetAsync()
        {
            var all = await _userService.GetAllAsync();

            // Build managers list (chỉ lấy Manager role)
            Managers = all
                .Where(u => u.RoleID == 2 || string.Equals(u.Role.RoleName, "Manager", StringComparison.OrdinalIgnoreCase))
                .Select(u => (u.UserID, $"{u.FullName} ({u.Email})"))
                .OrderBy(m => m.Item2)
                .ToList();

            // Apply filters
            var query = all.AsQueryable();
            if (FilterRoleId.HasValue)
                query = query.Where(u => u.RoleID == FilterRoleId.Value);
            if (!string.IsNullOrWhiteSpace(FilterUserCode))
            {
                var term = FilterUserCode.Trim().ToLowerInvariant();
                query = query.Where(u => (u.UserCode ?? "").ToLower().Contains(term));
            }
            if (!string.IsNullOrWhiteSpace(FilterUsername))
            {
                var term = FilterUsername.Trim().ToLowerInvariant();
                query = query.Where(u => (u.Username ?? "").ToLower().Contains(term));
            }
            if (FilterLocked.HasValue)
            {
                if (FilterLocked.Value)
                    query = query.Where(u => string.Equals(u.Status, "Locked", StringComparison.OrdinalIgnoreCase));
                else
                    query = query.Where(u => !string.Equals(u.Status, "Locked", StringComparison.OrdinalIgnoreCase));
            }

            Users = query
                .OrderBy(u => u.Username)
                .Select(u => new AdminUserControlViewModel
                {
                    UserID = u.UserID,
                    UserCode = u.UserCode ?? "",
                    Username = u.Username ?? "",
                    FullName = u.FullName ?? "",
                    Email = u.Email ?? "",
                    RoleID = u.RoleID,
                    RoleName = u.Role.RoleName,
                    IsDeleted = u.IsDeleted,
                    ForceChangePassword = u.ForceChangePassword,
                    TwoFAEnabled = u.TwoFAEnabled,
                    Status = u.Status,
                    ManagerID = u.ManagerID
                })
                .ToList();
        }

        // Lock/Unlock user (không áp dụng cho Admin)
        public async Task<IActionResult> OnPostToggleDeleteAsync(int id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null) return NotFound();

            if (IsAdminUser(user))
            {
                TempData["Error"] = "Không thể khóa/mở khóa tài khoản Admin.";
                return RedirectToPage();
            }

            user.Status = string.Equals(user.Status, "Locked", StringComparison.OrdinalIgnoreCase) ? "Active" : "Locked";
            user.UpdatedAt = DateTime.UtcNow;

            int? adminId = GetCurrentUserId();
            await _userService.UpdateUserAsync(user, adminId);
            TempData["Success"] = user.Status == "Locked" ? "Đã khoá tài khoản thành công." : "Đã mở khoá tài khoản thành công.";
            return RedirectToPage();
        }

        // Soft delete (không áp dụng cho Admin)
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy tài khoản.";
                return RedirectToPage();
            }

            if (IsAdminUser(user))
            {
                TempData["Error"] = "Không thể xóa tài khoản Admin.";
                return RedirectToPage();
            }

            int? adminId = GetCurrentUserId();

            if (!user.IsDeleted)
            {
                user.IsDeleted = true;
                user.Status = "Locked";
                user.UpdatedAt = DateTime.UtcNow;
                await _userService.UpdateUserAsync(user, adminId);
            }

            TempData["Success"] = "Xóa người dùng thành công.";
            return RedirectToPage();
        }

        // Force change password (không áp dụng cho Admin)
        public async Task<IActionResult> OnPostForceChangeAsync(int id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null) return NotFound();

            if (IsAdminUser(user))
            {
                TempData["Error"] = "Không thể yêu cầu đổi mật khẩu đối với tài khoản Admin.";
                return RedirectToPage();
            }

            if (user.IsDeleted)
            {
                TempData["Error"] = "Không thể yêu cầu đổi mật khẩu trên tài khoản đã xóa.";
                return RedirectToPage();
            }

            user.ForceChangePassword = true;
            user.UpdatedAt = DateTime.UtcNow;

            int? adminId = GetCurrentUserId();
            await _userService.UpdateUserAsync(user, adminId);

            TempData["Success"] = "Yêu cầu đổi mật khẩu đã được gửi đến người dùng.";
            return RedirectToPage();
        }

        // Create user (chỉ Employee / Manager)
        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Dữ liệu không hợp lệ.";
                return RedirectToPage();
            }

            if (CreateInput.RoleID != 1 && CreateInput.RoleID != 2)
            {
                TempData["Error"] = "Bạn chỉ được tạo Employee hoặc Manager.";
                return RedirectToPage();
            }

            if (CreateInput.RoleID == 1 && CreateInput.ManagerID == null)
            {
                TempData["Error"] = "Nhân viên bắt buộc chọn Manager.";
                return RedirectToPage();
            }

            var user = new User
            {
                Username = CreateInput.Username,
                Email = CreateInput.Email,
                FullName = CreateInput.FullName,
                Phone = CreateInput.Phone,
                RoleID = CreateInput.RoleID,
                ManagerID = CreateInput.ManagerID,
                Status = "Active",
                PasswordHash = PasswordHasher.HashPassword("demo"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            int? adminId = GetCurrentUserId();
            await _userService.AddUserAsync(user, adminId);

            TempData["Success"] = "Tạo người dùng thành công (mật khẩu mặc định: demo).";
            return RedirectToPage();
        }

        // Update Manager (không áp dụng cho Admin)
        public async Task<IActionResult> OnPostUpdateManagerAsync()
        {
            if (UpdateMgr.UserID <= 0)
            {
                TempData["Error"] = "Thiếu UserID.";
                return RedirectToPage();
            }

            var existing = await _userService.GetByIdAsync(UpdateMgr.UserID);
            if (existing == null) return NotFound();

            if (IsAdminUser(existing))
            {
                TempData["Error"] = "Không thể cập nhật Manager cho tài khoản Admin.";
                return RedirectToPage();
            }

            if (existing.IsDeleted)
            {
                TempData["Error"] = "Không thể cập nhật Manager cho tài khoản đã xóa.";
                return RedirectToPage();
            }

            existing.ManagerID = UpdateMgr.ManagerID;
            existing.UpdatedAt = DateTime.UtcNow;

            int? adminId = GetCurrentUserId();
            await _userService.UpdateUserAsync(existing, adminId);

            TempData["Success"] = "Cập nhật Manager thành công.";
            return RedirectToPage();
        }

        // Update Role (không áp dụng cho Admin; và không được đổi sang Admin)
        public async Task<IActionResult> OnPostUpdateRoleAsync()
        {
            if (!ModelState.IsValid || UpdateRole.UserID <= 0)
            {
                TempData["Error"] = "Thiếu thông tin cập nhật role.";
                return RedirectToPage();
            }

            if (UpdateRole.RoleID != 1 && UpdateRole.RoleID != 2)
            {
                TempData["Error"] = "Không được cấp quyền Admin hoặc role không hợp lệ.";
                return RedirectToPage();
            }

            var user = await _userService.GetByIdAsync(UpdateRole.UserID);
            if (user == null) return NotFound();

            if (IsAdminUser(user))
            {
                TempData["Error"] = "Không thể đổi role tài khoản Admin.";
                return RedirectToPage();
            }

            if (user.IsDeleted)
            {
                TempData["Error"] = "Không thể đổi role cho tài khoản đã xóa.";
                return RedirectToPage();
            }

            if (UpdateRole.RoleID == 1 && user.ManagerID == null)
            {
                TempData["Error"] = "Đổi sang Employee cần gán Manager trước.";
                return RedirectToPage();
            }

            user.RoleID = UpdateRole.RoleID;
            user.UpdatedAt = DateTime.UtcNow;

            int? adminId = GetCurrentUserId();
            await _userService.UpdateUserAsync(user, adminId);
            TempData["Success"] = "Cập nhật Role thành công.";
            return RedirectToPage();
        }

        private bool IsAdminUser(User u) =>
            u.RoleID == 3 || string.Equals(u.Role?.RoleName, "Admin", StringComparison.OrdinalIgnoreCase);

        private int? GetCurrentUserId()
        {
            var adminClaim = User.FindFirst("UserID")?.Value;
            if (!string.IsNullOrEmpty(adminClaim) && int.TryParse(adminClaim, out var parsed))
                return parsed;
            return null;
        }
    }

    public class CreateUserInput
    {
        [Required] public string Username { get; set; } = string.Empty;
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required] public string FullName { get; set; } = string.Empty;
        [Required] public int RoleID { get; set; }
        public int? ManagerID { get; set; }
        public string? Phone { get; set; }
    }

    public class UpdateManagerInput
    {
        [Required] public int UserID { get; set; }
        public int? ManagerID { get; set; }
    }

    public class UpdateRoleInput
    {
        [Required] public int UserID { get; set; }
        [Required] public int RoleID { get; set; }
    }

    public class AdminUserControlViewModel
    {
        public int UserID { get; set; }
        public string UserCode { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int RoleID { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
        public bool ForceChangePassword { get; set; }
        public bool TwoFAEnabled { get; set; }
        public int? ManagerID { get; set; }
    }
}
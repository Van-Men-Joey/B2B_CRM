using System.Security.Claims;
using AutoMapper;
using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Security;
using Customer_Relationship_Management.Services.Interfaces;
using Customer_Relationship_Management.ViewModels.User;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Customer_Relationship_Management.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly IMapper _mapper;

        public LoginModel(IUserService userService, IMapper mapper)
        {
            _userService = userService;
            _mapper = mapper;
        }

        // Form đăng nhập (đừng để validation của form này cản form modal)
        [BindProperty, ValidateNever]
        public string? UserCode { get; set; }

        [BindProperty, ValidateNever]
        public string? Password { get; set; }

        public string? ErrorMessage { get; set; }

        // Cờ để View mở modal sau POST đăng nhập
        public bool ShowForceChangeModal { get; set; } = false;

        // ĐÚNG YÊU CẦU: không bật modal ở GET
        public void OnGet()
        {
            // No-op
        }

        // Đăng nhập
        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(UserCode) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Vui lòng nhập đầy đủ UserCode và Password.";
                return Page();
            }

            var user = await _userService.GetByUserCodeAsync(UserCode);
            if (user == null)
            {
                ErrorMessage = "Thông tin đăng nhập không hợp lệ.";
                return Page();
            }

            if (user.IsDeleted)
            {
                ErrorMessage = "Tài khoản của bạn đã bị xóa hoặc khóa vĩnh viễn. Vui lòng liên hệ Admin.";
                return Page();
            }
            if (string.Equals(user.Status, "Locked", StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên để biết thêm chi tiết.";
                return Page();
            }

            if (!PasswordHasher.VerifyPassword(Password, user.PasswordHash))
            {
                ErrorMessage = "Thông tin đăng nhập không hợp lệ.";
                return Page();
            }

            var userVm = _mapper.Map<EmployeeProfileViewModel>(user);
            var roleName = userVm.RoleName ?? user.Role?.RoleName ?? "Employee";

            // Tạo cookie đăng nhập
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.FullName ?? user.UserCode),
                new Claim(ClaimTypes.Role, roleName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("UserCode", user.UserCode ?? string.Empty),
                new Claim("UserID", user.UserID.ToString())
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            // NGAY SAU KHI ĐĂNG NHẬP: nếu bị ép đổi mật khẩu thì trả về cùng trang + bật modal
            if (user.ForceChangePassword)
            {
                ShowForceChangeModal = true;
                TempData["ForceChangePasswordMsg"] = "Bạn được yêu cầu đổi mật khẩu. Vui lòng cập nhật bên dưới.";
                return Page(); // không redirect, đúng yêu cầu “chỉ khi vừa nhập usercode/password”
            }

            // Không bị ép -> vào dashboard
            return RedirectByRole(roleName);
        }

        // Đổi mật khẩu trong modal (bắt buộc)
        public async Task<IActionResult> OnPostChangePasswordAsync(string OldPassword, string NewPassword, string ConfirmPassword)
        {
            // Loại bỏ validate của form login để không dính lỗi "UserCode/Password is required"
            ModelState.Remove(nameof(UserCode));
            ModelState.Remove(nameof(Password));

            var userIdClaim = User.FindFirst("UserID")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Challenge();

            var user = await _userService.GetByIdAsync(userId);
            if (user == null) return NotFound();

            // (Bắt buộc) xác thực lại mật khẩu cũ
            if (!PasswordHasher.VerifyPassword(OldPassword, user.PasswordHash))
                ModelState.AddModelError(string.Empty, "Mật khẩu cũ không chính xác.");

            if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 6)
                ModelState.AddModelError(string.Empty, "Mật khẩu mới phải có ít nhất 6 ký tự.");

            if (NewPassword != ConfirmPassword)
                ModelState.AddModelError(string.Empty, "Xác nhận mật khẩu không khớp.");

            if (!ModelState.IsValid)
            {
                // Giữ modal mở để người dùng sửa
                ShowForceChangeModal = true;
                TempData["ForceChangePasswordMsg"] = "Vui lòng sửa lỗi bên dưới và thử lại.";
                return Page();
            }

            // Cập nhật mật khẩu và gỡ cờ force
            user.PasswordHash = PasswordHasher.HashPassword(NewPassword);
            user.ForceChangePassword = false; // Admin yêu cầu -> đổi xong thì gỡ
            user.UpdatedAt = DateTime.UtcNow;
            await _userService.UpdateUserAsync(user, user.UserID);

            TempData["Success"] = "Đổi mật khẩu thành công.";

            var roleName = User.FindFirst(ClaimTypes.Role)?.Value ?? "Employee";
            return RedirectByRole(roleName);
        }

        private IActionResult RedirectByRole(string roleName) =>
            roleName switch
            {
                "Admin" => RedirectToPage("/Admin/Dashboard"),
                "Manager" => RedirectToPage("/Manager/Dashboard"),
                "Employee" => RedirectToPage("/Employee/Dashboard"),
                _ => RedirectToPage("/Employee/Dashboard")
            };
    }
}
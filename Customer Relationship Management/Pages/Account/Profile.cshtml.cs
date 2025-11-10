using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Repositories.Interfaces;
using Customer_Relationship_Management.ViewModels.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Customer_Relationship_Management.Pages.Account
{
    [Authorize]
    public class ProfileModel : PageModel
    {
        private readonly IUserRepository _userRepository;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public ProfileModel(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [BindProperty]
        public UserProfileViewModel Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            // 🧠 Lấy email từ Claims (người đăng nhập)
            var email = User.Identity?.Name;
            if (email == null)
                return RedirectToPage("/Account/Login");

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                return NotFound();

            Input = new UserProfileViewModel
            {
                UserID = user.UserID,
                UserCode = user.UserCode,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                AvatarPath = user.UserCode + ".jpg", // ví dụ, nếu bạn có upload avatar
                RoleName = user.Role.RoleName,
                Status = user.Status
            };

            return Page();
        }

        // 🧩 Cập nhật thông tin cá nhân
        public async Task<IActionResult> OnPostUpdateAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var email = User.Identity?.Name;
            if (email == null)
                return RedirectToPage("/Account/Login");

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                return NotFound();

            user.FullName = Input.FullName;
            user.Email = Input.Email;
            user.Phone = Input.Phone;
            user.UpdatedAt = DateTime.Now;

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
            return RedirectToPage();
        }

        // 🔐 Đổi mật khẩu
        public async Task<IActionResult> OnPostChangePasswordAsync()
        {
            if (string.IsNullOrWhiteSpace(Input.OldPassword) || string.IsNullOrWhiteSpace(Input.NewPassword))
            {
                ModelState.AddModelError(string.Empty, "Vui lòng nhập đầy đủ mật khẩu cũ và mới.");
                return Page();
            }

            var email = User.Identity?.Name;
            if (email == null)
                return RedirectToPage("/Account/Login");

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                return NotFound();

            var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, Input.OldPassword);
            if (verifyResult == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(string.Empty, "Mật khẩu cũ không chính xác.");
                return Page();
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, Input.NewPassword);
            user.UpdatedAt = DateTime.Now;

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            return RedirectToPage();
        }
    }
}

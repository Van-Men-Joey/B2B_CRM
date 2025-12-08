using Customer_Relationship_Management.Data;
using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Customer_Relationship_Management.Pages.Director.Profile
{
    [Authorize(Roles = "Director")]
    public class IndexModel : PageModel
    {
        private readonly B2BDbContext _context;

        public IndexModel(B2BDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public User CurrentUser { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdStr = User.FindFirst("UserID")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int uid))
                return RedirectToPage("/Account/Login");

            var user = await _context.Users.FindAsync(uid);
            if (user == null) return RedirectToPage("/Account/Login");

            CurrentUser = user;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userIdStr = User.FindFirst("UserID")?.Value;
            if (!int.TryParse(userIdStr, out int uid)) return RedirectToPage("/Account/Login");

            var user = await _context.Users.FindAsync(uid);
            if (user == null) return RedirectToPage("/Account/Login");

            user.FullName = CurrentUser.FullName;
            user.Phone = CurrentUser.Phone;
            user.UpdatedAt = DateTime.UtcNow;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật hồ sơ thành công.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostChangePasswordAsync(string OldPassword, string NewPassword, string ConfirmPassword)
        {
            var userIdStr = User.FindFirst("UserID")?.Value;
            if (!int.TryParse(userIdStr, out int uid)) return RedirectToPage("/Account/Login");

            var user = await _context.Users.FindAsync(uid);
            if (user == null) return RedirectToPage("/Account/Login");

            if (!PasswordHasher.VerifyPassword(OldPassword, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Mật khẩu cũ không đúng.");
                return Page();
            }

            if (NewPassword != ConfirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Xác nhận mật khẩu không khớp.");
                return Page();
            }

            user.PasswordHash = PasswordHasher.HashPassword(NewPassword);
            user.ForceChangePassword = false;
            user.UpdatedAt = DateTime.UtcNow;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đổi mật khẩu thành công.";
            return RedirectToPage();
        }
    }
}
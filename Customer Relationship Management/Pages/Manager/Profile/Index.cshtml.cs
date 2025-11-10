using Customer_Relationship_Management.Data;
using Customer_Relationship_Management.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Customer_Relationship_Management.Security;

namespace Customer_Relationship_Management.Pages.Manager.Profile
{
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
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
                return RedirectToPage("/Account/Login");

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
                return RedirectToPage("/Account/Login");

            CurrentUser = user;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return RedirectToPage("/Account/Login");

            user.FullName = CurrentUser.FullName;
            user.Phone = CurrentUser.Phone;
            user.UpdatedAt = DateTime.UtcNow;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật thông tin thành công!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostChangePasswordAsync(string OldPassword, string NewPassword, string ConfirmPassword)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return RedirectToPage("/Account/Login");

            if (!PasswordHasher.VerifyPassword(OldPassword, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Mật khẩu cũ không chính xác!");
                return Page();
            }

            if (NewPassword != ConfirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Mật khẩu xác nhận không khớp!");
                return Page();
            }

            user.PasswordHash = PasswordHasher.HashPassword(NewPassword);
            // Voluntary change: do not set ForceChangePassword to true
            user.ForceChangePassword = false;
            user.UpdatedAt = DateTime.UtcNow;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đổi mật khẩu thành công!";
            return RedirectToPage();
        }
    }
}
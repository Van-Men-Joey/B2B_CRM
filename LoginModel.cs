csharp Customer Relationship Management\Pages\Account\Login.cshtml.cs
using AutoMapper;
using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Services.Interfaces;
using Customer_Relationship_Management.Security; // ?? dùng PasswordHasher
using Customer_Relationship_Management.ViewModels.User; // ch?a EmployeeProfileViewModel
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Customer_Relationship_Management.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly IMapper _mapper;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(IUserService userService, IMapper mapper, ILogger<LoginModel> logger)
        {
            _userService = userService;
            _mapper = mapper;
            _logger = logger;
        }

        [BindProperty]
        public string UserCode { get; set; } = null!;

        [BindProperty]
        public string Password { get; set; } = null!;

        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
            // load page l?n ??u
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // normalize input early
            var code = UserCode?.Trim();
            var pwd = Password ?? string.Empty;

            _logger.LogInformation("Login attempt for UserCode='{UserCodeProvided}'", code);

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(pwd))
            {
                ErrorMessage = "Please enter both UserCode and Password";
                _logger.LogWarning("Missing credentials (UserCode or Password empty).");
                return Page();
            }

            // L?y user t? DB qua service theo UserCode (nên Include Role ? repository)
            var user = await _userService.GetByUserCodeAsync(code);
            if (user == null)
            {
                ErrorMessage = "Invalid login";
                _logger.LogWarning("No user found for UserCode='{UserCode}'", code);
                return Page();
            }

            _logger.LogDebug("Found user {UserID} with RoleID={RoleID}", user.UserID, user.RoleID);

            // Ki?m tra password hash (BCrypt)
            bool ok;
            try
            {
                ok = PasswordHasher.VerifyPassword(pwd, user.PasswordHash);
            }
            catch (Exception ex)
            {
                // unexpected: maybe stored hash corrupted
                _logger.LogError(ex, "Password verification threw for user {UserID}", user.UserID);
                ErrorMessage = "Invalid login";
                return Page();
            }

            _logger.LogInformation("Password verification for user {UserID}: {Result}", user.UserID, ok);

            if (!ok)
            {
                ErrorMessage = "Invalid login";
                return Page();
            }

            // Map sang ViewModel ?? dùng trong profile page
            var userVm = _mapper.Map<EmployeeProfileViewModel>(user);

            // L?y RoleName t? ViewModel (ho?c user.Role)
            var roleName = userVm.RoleName ?? user.Role?.RoleName ?? "Employee";

            // N?u login thành công: t?o Claims & Cookie
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.FullName ?? user.UserCode),
                new Claim(ClaimTypes.Role, roleName),
                // l?u thêm ID ?? ti?n l?y ? các page khác
                new Claim("UserID", user.UserID.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));

            _logger.LogInformation("User {UserID} signed in, role={Role}", user.UserID, roleName);

            // chuy?n v? trang Index (có [Authorize])
            switch (roleName)
            {
                case "Admin":
                    return RedirectToPage("/Admin/Dashboard");
                case "Manager":
                    return RedirectToPage("/Manager/Dashboard");
                case "Employee":
                    return RedirectToPage("/Employee/Dashboard");
                default:
                    return RedirectToPage("/Account/Login");
            }
        }
    }
}
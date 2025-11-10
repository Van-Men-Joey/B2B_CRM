using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Customer_Relationship_Management.Pages.Employee.Deal
{
    [Authorize(Roles = "Employee")]
    public class DeleteModel : PageModel
    {
        private readonly IDealService _dealService;
        private readonly ILogger<DeleteModel> _logger;

        public DeleteModel(IDealService dealService, ILogger<DeleteModel> logger)
        {
            _dealService = dealService;
            _logger = logger;
        }

        [BindProperty]
        public Models.Deal? Deal { get; set; }

        // ========================= GET =========================
        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst("UserID")?.Value;
                if (userIdClaim == null)
                {
                    _logger.LogWarning("🚫 Người dùng chưa đăng nhập khi truy cập trang xóa deal.");
                    return RedirectToPage("/Account/Login");
                }

                int employeeId = int.Parse(userIdClaim);

                // Lấy deal kèm thông tin khách hàng
                Deal = await _dealService.GetDealByIdAsync(id, employeeId);
                if (Deal == null)
                {
                    _logger.LogWarning("⚠️ Không tìm thấy deal ID {Id} hoặc không có quyền truy cập.", id);
                    return NotFound("Không tìm thấy deal hoặc bạn không có quyền xóa.");
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi tải dữ liệu deal để xóa. DealID: {Id}", id);
                TempData["ErrorMessage"] = "Lỗi hệ thống khi tải dữ liệu deal.";
                return RedirectToPage("/Employee/Deal/Index");
            }
        }

        // ========================= POST =========================
        public async Task<IActionResult> OnPostAsync(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst("UserID")?.Value;
                if (userIdClaim == null)
                {
                    _logger.LogWarning("🚫 Người dùng chưa đăng nhập khi cố gắng xóa deal.");
                    return RedirectToPage("/Account/Login");
                }

                int employeeId = int.Parse(userIdClaim);

                var (success, message) = await _dealService.SoftDeleteAsync(id, employeeId);

                // Ghi log nghiệp vụ
                var logData = new
                {
                    DealID = id,
                    EmployeeID = employeeId,
                    Result = success,
                    Message = message
                };
                _logger.LogInformation("🧾 Kết quả xóa deal: {Json}", JsonSerializer.Serialize(logData));

                if (!success)
                {
                    TempData["ErrorMessage"] = message;
                    return RedirectToPage("/Employee/Deal/Index");
                }

                TempData["SuccessMessage"] = message;
                return RedirectToPage("/Employee/Deal/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi xử lý xóa deal. DealID: {Id}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa deal.";
                return RedirectToPage("/Employee/Deal/Index");
            }
        }
    }
}

using System.Security.Claims;
using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Customer_Relationship_Management.Pages.Manager.Deal
{
    /// <summary>
    /// Trang quản lý tổng quan Deal cho Manager (Deal Supervision Dashboard)
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly IDealService _dealService;
        private readonly IUserService _userService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            IDealService dealService,
            IUserService userService,
            ILogger<IndexModel> logger)
        {
            _dealService = dealService;
            _userService = userService;
            _logger = logger;
        }

        // =========================================
        // 🔹 DỮ LIỆU HIỂN THỊ TRÊN DASHBOARD
        // =========================================
        public IEnumerable<Models.Deal> TeamDeals { get; set; } = new List<Models.Deal>();
        public IEnumerable<User> TeamMembers { get; set; } = new List<User>();
        public IDictionary<string, decimal> PipelineSummary { get; set; } = new Dictionary<string, decimal>();

        // Danh sách file tài liệu deal (nếu cần tải)
        public IEnumerable<string> DealFiles { get; set; } = new List<string>();

        // =========================================
        // 🔹 THUỘC TÍNH LỌC / TÌM KIẾM
        // =========================================
        [BindProperty(SupportsGet = true)]
        public string? Stage { get; set; }

        [BindProperty(SupportsGet = true)]
        public decimal? MinValue { get; set; }

        [BindProperty(SupportsGet = true)]
        public decimal? MaxValue { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DeadlineBefore { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SelectedEmployeeId { get; set; }  // Lọc theo nhân viên cụ thể

        // =========================================
        // 🔹 THUỘC TÍNH XỬ LÝ FORM CHUYỂN DEAL
        // =========================================
        [BindProperty]
        public int ReassignDealId { get; set; }

        [BindProperty]
        public int NewEmployeeId { get; set; }

        // =========================================
        // 🔹 HÀM LOAD CHÍNH
        // =========================================
        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var claim = User.FindFirst("UserID");
                if (claim == null)
                    return RedirectToPage("/Account/Login");

                int managerId = int.Parse(claim.Value);

                // 1️⃣ Lấy danh sách nhân viên trong team
                TeamMembers = await _userService.GetEmployeesByManagerAsync(managerId);

                // 2️⃣ Lấy danh sách deal toàn team (hoặc đã lọc)
                if (!string.IsNullOrEmpty(Stage) || MinValue.HasValue || MaxValue.HasValue || DeadlineBefore.HasValue)
                {
                    TeamDeals = await _dealService.FilterTeamDealsAsync(
                        managerId,
                        Stage,
                        MinValue,
                        MaxValue,
                        DeadlineBefore
                    );
                }
                else
                {
                    TeamDeals = await _dealService.GetTeamDealsAsync(managerId);
                }

                // Nếu chọn nhân viên cụ thể → lọc theo AssignedToUserID (đã đổi)
                if (SelectedEmployeeId.HasValue)
                {
                    TeamDeals = TeamDeals.Where(d => d.Customer.AssignedToUserID == SelectedEmployeeId.Value);
                }

                // 3️⃣ Lấy thống kê pipeline theo stage
                PipelineSummary = await _dealService.GetTeamPipelineSummaryAsync(managerId);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi tải trang quản lý deal: {Message}", ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi tải dữ liệu. Vui lòng thử lại sau.";
                return Page();
            }
        }

        // =========================================
        // 🔹 CHUYỂN NHÂN VIÊN PHỤ TRÁCH DEAL
        // =========================================
        public async Task<IActionResult> OnPostReassignDealAsync()
        {
            try
            {
                var claim = User.FindFirst("UserID");
                if (claim == null)
                    return RedirectToPage("/Account/Login");

                int managerId = int.Parse(claim.Value);

                if (ReassignDealId <= 0 || NewEmployeeId <= 0)
                {
                    TempData["Error"] = "Thông tin chuyển giao không hợp lệ.";
                    return RedirectToPage();
                }

                var result = await _dealService.ReassignDealAsync(ReassignDealId, NewEmployeeId, managerId);
                TempData[result.Success ? "Success" : "Error"] = result.Message;

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi chuyển giao deal {DealID}: {Message}", ReassignDealId, ex.Message);
                TempData["Error"] = "Không thể chuyển giao deal. Vui lòng thử lại.";
                return RedirectToPage();
            }
        }

        // =========================================
        // 🔹 XEM / TẢI TÀI LIỆU DEAL
        // =========================================
        public async Task<IActionResult> OnGetViewFilesAsync(int dealId)
        {
            try
            {
                DealFiles = await _dealService.GetDealFilesAsync(dealId);
                if (!DealFiles.Any())
                {
                    TempData["Error"] = "Không có tài liệu nào được upload cho deal này.";
                }
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi tải file deal {DealID}: {Message}", dealId, ex.Message);
                TempData["Error"] = "Không thể tải danh sách file.";
                return RedirectToPage();
            }
        }

        // =========================================
        // 🔹 TẢI FILE CỤ THỂ
        // =========================================
        public async Task<IActionResult> OnGetDownloadFileAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
                {
                    TempData["Error"] = "File không tồn tại hoặc đã bị xóa.";
                    return RedirectToPage();
                }

                var fileName = Path.GetFileName(filePath);
                var mimeType = "application/octet-stream";
                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);

                return File(fileBytes, mimeType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi tải file: {Message}", ex.Message);
                TempData["Error"] = "Không thể tải file. Vui lòng thử lại.";
                return RedirectToPage();
            }
        }
    }
}

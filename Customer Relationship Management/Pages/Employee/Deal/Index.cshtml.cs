using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Customer_Relationship_Management.Pages.Employee.Deal
{
    [Authorize(Roles = "Employee")]
    public class IndexModel : PageModel
    {
        private readonly IDealService _dealService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IDealService dealService, ILogger<IndexModel> logger)
        {
            _dealService = dealService;
            _logger = logger;
        }

        public IEnumerable<Models.Deal> Deals { get; set; } = new List<Models.Deal>();

        // --- Các filter ---
        [BindProperty(SupportsGet = true)] public string? StageFilter { get; set; }
        [BindProperty(SupportsGet = true)] public string? Keyword { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? FromDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? ToDate { get; set; }

        // --- Phân trang ---
        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
        public int TotalPages { get; set; }
        private const int PageSize = 8;

        // --- Xử lý GET ---
        public async Task<IActionResult> OnGetAsync()
        {
            var userIdClaim = User.FindFirst("UserID")?.Value;
            if (userIdClaim == null)
                return RedirectToPage("/Account/Login");

            int employeeId = int.Parse(userIdClaim);

            IEnumerable<Models.Deal> allDeals;

            // --- Lọc theo từ khóa, giai đoạn hoặc tất cả ---
            if (!string.IsNullOrEmpty(Keyword))
            {
                allDeals = await _dealService.SearchDealsAsync(employeeId, Keyword);
            }
            else if (!string.IsNullOrEmpty(StageFilter))
            {
                allDeals = await _dealService.GetDealsByStageAsync(StageFilter, employeeId);
            }
            else
            {
                allDeals = await _dealService.GetDealsByEmployeeAsync(employeeId);
            }

            // --- Lọc theo thời gian ---
            if (FromDate.HasValue)
                allDeals = allDeals.Where(d => d.Deadline >= FromDate.Value);

            if (ToDate.HasValue)
                allDeals = allDeals.Where(d => d.Deadline <= ToDate.Value);

            // --- Phân trang ---
            int totalDeals = allDeals.Count();
            TotalPages = (int)Math.Ceiling(totalDeals / (double)PageSize);
            PageNumber = Math.Max(1, Math.Min(PageNumber, TotalPages == 0 ? 1 : TotalPages));

            Deals = allDeals
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            return Page();
        }

        // --- Xử lý POST: Cập nhật giai đoạn của Deal ---
        [BindProperty] public int DealIdToUpdate { get; set; }
        [BindProperty] public string? NewStage { get; set; }

        public async Task<IActionResult> OnPostUpdateStageAsync()
        {
            _logger.LogInformation("📥 Nhận yêu cầu cập nhật Deal ID {DealId} sang giai đoạn {Stage}", DealIdToUpdate, NewStage);

            var userIdClaim = User.FindFirst("UserID")?.Value;
            if (userIdClaim == null)
                return RedirectToPage("/Account/Login");

            int employeeId = int.Parse(userIdClaim);
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                _logger.LogWarning("⚠️ ModelState không hợp lệ: {Errors}", errors);
                return BadRequest(new { error = errors });
            }

            if (DealIdToUpdate <= 0 || string.IsNullOrEmpty(NewStage))
            {
                TempData["ErrorMessage"] = "Dữ liệu cập nhật không hợp lệ.";
                return RedirectToPage();
            }

            try
            {
                bool result = await _dealService.UpdateDealStageAsync(DealIdToUpdate, NewStage, employeeId);
                if (result)
                    TempData["SuccessMessage"] = "✅ Cập nhật giai đoạn Deal thành công.";
                else
                    TempData["ErrorMessage"] = "❌ Không thể cập nhật giai đoạn Deal.";
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Người dùng không có quyền thay đổi Deal ID {DealId}", DealIdToUpdate);
                TempData["ErrorMessage"] = "⚠️ Bạn không có quyền thay đổi giai đoạn của Deal này.";
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Giai đoạn không hợp lệ: {Message}", ex.Message);
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật giai đoạn Deal ID {DealId}: {Message}", DealIdToUpdate, ex.Message);
                TempData["ErrorMessage"] = "❌ Đã xảy ra lỗi trong quá trình cập nhật.";
            }


            return RedirectToPage();
        }
    }
}

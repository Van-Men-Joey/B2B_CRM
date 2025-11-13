using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Repositories.Interfaces;
using Customer_Relationship_Management.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Customer_Relationship_Management.Security; // thêm để dùng enum

namespace Customer_Relationship_Management.Pages.Manager.Contract
{
    [Authorize(Roles = "Manager")]
    public class IndexModel : PageModel
    {
        private readonly IContractService _contractService;
        private readonly IUserRepository _userRepo;

        public IndexModel(IContractService contractService, IUserRepository userRepo)
        {
            _contractService = contractService;
            _userRepo = userRepo;
        }

        public List<Models.Contract> Contracts { get; set; } = new();
        public string CurrentStatus { get; set; } = "Pending";
        public Models.Contract? SelectedContract { get; set; }

        // --- Filters (GET-binding) ---
        [BindProperty(SupportsGet = true)] public DateTime? FromDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? ToDate { get; set; }
        [BindProperty(SupportsGet = true)] public string? Keyword { get; set; }

        public async Task OnGetAsync(string? status)
        {
            await LoadDataAsync(status);
        }

        public async Task<IActionResult> OnPostApproveAsync(int id)
        {
            var userIdClaim = User.FindFirst("UserID")?.Value;
            int? approverId = null;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var parsed)) approverId = parsed;
            else
            {
                var userCode = User.FindFirst("UserCode")?.Value;
                var manager = await _userRepo.GetByUserCodeAsync(userCode ?? string.Empty);
                approverId = manager?.UserID;
            }

            if (approverId == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin người duyệt.";
                return RedirectToPage();
            }

            try
            {
                await _contractService.ApproveAsync(id, ContractApprovalStatus.Approved.ToString(), approverId);
                TempData["SuccessMessage"] = $"✅ Hợp đồng #{id} đã được phê duyệt.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi phê duyệt: {ex.Message}";
            }

            return RedirectToPage(new { status = "Pending" });
        }

        public async Task<IActionResult> OnPostRejectAsync(int id)
        {
            var userIdClaim = User.FindFirst("UserID")?.Value;
            int? approverId = null;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var parsed)) approverId = parsed;
            else
            {
                var userCode = User.FindFirst("UserCode")?.Value;
                var manager = await _userRepo.GetByUserCodeAsync(userCode ?? string.Empty);
                approverId = manager?.UserID;
            }

            if (approverId == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin người duyệt.";
                return RedirectToPage();
            }

            try
            {
                await _contractService.ApproveAsync(id, ContractApprovalStatus.Rejected.ToString(), approverId);
                TempData["ErrorMessage"] = $"❌ Hợp đồng #{id} đã bị từ chối.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi từ chối: {ex.Message}";
            }

            return RedirectToPage(new { status = "Pending" });
        }

        public async Task<IActionResult> OnPostLoadDetailsAsync(int id, string? status)
        {
            // Load list and selected contract, stay on same page, then JS will open modal
            await LoadDataAsync(status);
            SelectedContract = await _contractService.GetByIdAsync(id);
            return Page();
        }

        private async Task LoadDataAsync(string? status)
        {
            var userCode = User.FindFirst("UserCode")?.Value;
            var manager = await _userRepo.GetByUserCodeAsync(userCode ?? string.Empty);
            if (manager == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy Manager hiện tại.";
                return;
            }

            CurrentStatus = string.IsNullOrEmpty(status) ? "Pending" : status;
            var list = (await _contractService.GetByStatusAsync(CurrentStatus)).ToList();

            // Apply filters in-memory (source already includes relations)
            if (FromDate.HasValue)
                list = list.Where(x => x.CreatedAt.Date >= FromDate.Value.Date).ToList();
            if (ToDate.HasValue)
                list = list.Where(x => x.CreatedAt.Date <= ToDate.Value.Date).ToList();
            if (!string.IsNullOrWhiteSpace(Keyword))
            {
                var kw = Keyword.Trim().ToLowerInvariant();
                list = list.Where(x =>
                        (x.Deal?.DealName ?? string.Empty).ToLower().Contains(kw) ||
                        (x.CreatedBy?.FullName ?? string.Empty).ToLower().Contains(kw) ||
                        (x.ContractContent ?? string.Empty).ToLower().Contains(kw))
                    .ToList();
            }

            Contracts = list;
        }
    }
}
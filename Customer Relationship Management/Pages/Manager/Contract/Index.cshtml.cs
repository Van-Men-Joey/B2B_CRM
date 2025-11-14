using System.Linq;
using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Repositories.Interfaces;
using Customer_Relationship_Management.Security;
using Customer_Relationship_Management.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
// Alias namespace để dùng STT.Task và STT.Task<TResult>
using STT = System.Threading.Tasks;

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

        // Filters
        [BindProperty(SupportsGet = true)] public DateTime? FromDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? ToDate { get; set; }
        [BindProperty(SupportsGet = true)] public string? Keyword { get; set; }

        // GET: /Manager/Contract/{status?}/{id?}
        public async STT.Task OnGetAsync(string? status, int? id)
        {
            await LoadDataAsync(status);
            if (id.HasValue && id.Value > 0)
            {
                SelectedContract = await _contractService.GetByIdAsync(id.Value);
            }
        }

        public async STT.Task<IActionResult> OnPostApproveAsync(int id, string? status, int? selectedId)
        {
            var approverId = await ResolveApproverIdAsync();
            if (approverId == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin người duyệt.";
                return RedirectToPage(new { status = status ?? "Pending", id = selectedId });
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

            return RedirectToPage(new { status = status ?? "Pending", id = id }); // giữ modal mở, show cập nhật
        }

        public async STT.Task<IActionResult> OnPostRejectAsync(int id, string? status, int? selectedId)
        {
            var approverId = await ResolveApproverIdAsync();
            if (approverId == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin người duyệt.";
                return RedirectToPage(new { status = status ?? "Pending", id = selectedId });
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

            return RedirectToPage(new { status = status ?? "Pending", id = id });
        }

        private async STT.Task<int?> ResolveApproverIdAsync()
        {
            var userIdClaim = User.FindFirst("UserID")?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var parsed)) return parsed;

            var userCode = User.FindFirst("UserCode")?.Value;
            var manager = await _userRepo.GetByUserCodeAsync(userCode ?? string.Empty);
            return manager?.UserID;
        }

        private async STT.Task LoadDataAsync(string? status)
        {
            CurrentStatus = string.IsNullOrEmpty(status) ? "Pending" : status;

            var list = (await _contractService.GetByStatusAsync(CurrentStatus)).ToList();

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
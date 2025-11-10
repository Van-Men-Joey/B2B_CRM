using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Repositories.Interfaces;
using Customer_Relationship_Management.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Customer_Relationship_Management.Pages.Manager.Contract
{
    [Authorize(Roles = "Manager")]
    public class DetailsModel : PageModel
    {
        private readonly IContractService _contractService;
        private readonly IUserRepository _userRepo;

        public DetailsModel(IContractService contractService, IUserRepository userRepo)
        {
            _contractService = contractService;
            _userRepo = userRepo;
        }

        public Models.Contract Contract { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var contract = await _contractService.GetByIdAsync(id);
            if (contract == null)
            {
                return RedirectToPage("/Manager/Contract/Index");
            }

            Contract = contract;
            return Page();
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

            if (approverId == null) return NotFound();

            try
            {
                await _contractService.ApproveAsync(id, "Approved", approverId);
                TempData["SuccessMessage"] = "✅ Hợp đồng đã được duyệt thành công.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi phê duyệt: {ex.Message}";
            }

            return RedirectToPage("/Manager/Contract/Index");
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

            if (approverId == null) return NotFound();

            try
            {
                await _contractService.ApproveAsync(id, "Rejected", approverId);
                TempData["WarningMessage"] = "❌ Hợp đồng đã bị từ chối.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi từ chối: {ex.Message}";
            }

            return RedirectToPage("/Manager/Contract/Index");
        }
    }
}
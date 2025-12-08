using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Customer_Relationship_Management.Pages.Director.Contract
{
    [Authorize(Roles = "Director")]
    public class IndexModel : PageModel
    {
        private readonly IContractService _contractService;

        public IndexModel(IContractService contractService)
        {
            _contractService = contractService;
        }

        public IEnumerable<Models.Contract> Contracts { get; set; } = new List<Models.Contract>();

        [BindProperty(SupportsGet = true)]
        public string ActiveTab { get; set; } = "ToSign"; // ToSign hoặc History

        public async Task OnGetAsync()
        {
            // Tab "Cần ký": Lấy các HĐ trạng thái "ManagerApproved"
            if (ActiveTab == "ToSign")
            {
                Contracts = await _contractService.GetByStatusAsync("ManagerApproved");
            }
            // Tab "Lịch sử": Lấy các HĐ đã Approved (hoặc Rejected bởi Director - cần xử lý thêm nếu muốn)
            else
            {
                Contracts = await _contractService.GetByStatusAsync("Approved");
            }
        }

        public async Task<IActionResult> OnPostSignAsync(int id)
        {
            var userIdStr = User.FindFirst("UserID")?.Value;
            if (!int.TryParse(userIdStr, out int directorId)) return Unauthorized();

            try
            {
                await _contractService.DirectorSignAsync(id, directorId);
                TempData["Success"] = $"Đã ký duyệt hợp đồng #{id} thành công.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToPage(new { ActiveTab = "ToSign" });
        }

        public async Task<IActionResult> OnPostRejectAsync(int id)
        {
            var userIdStr = User.FindFirst("UserID")?.Value;
            if (!int.TryParse(userIdStr, out int directorId)) return Unauthorized();

            // Director từ chối -> Quay về Rejected (hoặc có thể trả về Pending để sửa lại)
            await _contractService.ApproveAsync(id, "Rejected", directorId);
            TempData["Success"] = $"Đã từ chối hợp đồng #{id}.";

            return RedirectToPage(new { ActiveTab = "ToSign" });
        }
    }
}
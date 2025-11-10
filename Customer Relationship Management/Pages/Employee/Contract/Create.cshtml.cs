using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Repositories.Interfaces;
using Customer_Relationship_Management.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Customer_Relationship_Management.Pages.Employee.Contract
{
    [Authorize(Roles = "Employee")]
    public class CreateModel : PageModel
    {
        private readonly IContractService _contractService;
        private readonly IDealRepository _dealRepo;
        private readonly IUserRepository _userRepo;

        // Đổi tên property sang ContractModel để không trùng tên với namespace
        [BindProperty]
        public global::Customer_Relationship_Management.Models.Contract ContractModel { get; set; } = new();
        [BindProperty]
        public IFormFile? UploadFile { get; set; }
        public List<Customer_Relationship_Management.Models.Deal> Deals { get; set; } = new();

        public CreateModel(IContractService contractService, IDealRepository dealRepo, IUserRepository userRepo)
        {
            _contractService = contractService;
            _dealRepo = dealRepo;
            _userRepo = userRepo;
        }

        public async System.Threading.Tasks.Task OnGetAsync()
        {
            var userCode = User.FindFirst("UserCode")?.Value;
            if (string.IsNullOrEmpty(userCode))
            {
                RedirectToPage("/Account/Login");
                return;
            }

            var currentUser = await _userRepo.GetByUserCodeAsync(userCode);
            if (currentUser == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng hiện tại.";
                RedirectToPage("/Error");
                return;
            }

            Deals = (await _dealRepo.GetDealsByEmployeeAsync(currentUser.UserID)).ToList();
        }

        public async System.Threading.Tasks.Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            var userCode = User.FindFirst("UserCode")?.Value;
            if (string.IsNullOrEmpty(userCode))
            {
                TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToPage("/Account/Login");
            }

            var currentUser = await _userRepo.GetByUserCodeAsync(userCode);
            if (currentUser == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng hiện tại.";
                return RedirectToPage("/Error");
            }

            ContractModel.CreatedByUserID = currentUser.UserID;
            ContractModel.ApprovalStatus = "Pending";
            ContractModel.CreatedAt = DateTime.UtcNow;
            ContractModel.UpdatedAt = DateTime.UtcNow;

            if (UploadFile != null)
            {
                var folderPath = Path.Combine("wwwroot", "uploads", "contracts");
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                var filePath = Path.Combine(folderPath, UploadFile.FileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await UploadFile.CopyToAsync(stream);
                ContractModel.FilePath = "/uploads/contracts/" + UploadFile.FileName;
            }

            // Gọi service để tạo và tự động ghi Audit Log
            await _contractService.CreateAsync(ContractModel, currentUser.UserID);

            TempData["SuccessMessage"] = "Tạo hợp đồng thành công! Chờ Manager duyệt.";
            return RedirectToPage("/Employee/Contract/Index");
        }
    }
}
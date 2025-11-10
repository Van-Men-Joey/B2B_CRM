using Microsoft.AspNetCore.Mvc.RazorPages;
using Customer_Relationship_Management.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;

namespace Customer_Relationship_Management.Pages.Employee.Contract
{
    [Authorize(Roles = "Employee")]
    public class IndexModel : PageModel
    {
        private readonly IContractRepository _contractRepo;
        private readonly IUserRepository _userRepo;
        private readonly IConfiguration _config;

        // Danh sách hợp đồng cho Razor: Model.Contract
        public List<Models.Contract> Contract { get; set; } = new();

        // Bộ lọc (giống trang Deal)
        [BindProperty(SupportsGet = true)] public string? StatusFilter { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? FromDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? ToDate { get; set; }
        [BindProperty(SupportsGet = true)] public string? Keyword { get; set; }

        // Cấu hình thanh toán (VietQR)
        public string BankCode { get; set; } = "vcb";           // Vietcombank mặc định
        public string BankAccount { get; set; } = "0000000000"; // thay trong appsettings
        public string BankAccountName { get; set; } = "B2B CRM"; // thay trong appsettings

        public IndexModel(IContractRepository contractRepo, IUserRepository userRepo, IConfiguration config)
        {
            _contractRepo = contractRepo;
            _userRepo = userRepo;
            _config = config;
        }

        public async System.Threading.Tasks.Task<IActionResult> OnGetAsync()
        {
            // Load cấu hình thanh toán nếu có
            BankCode = _config["Payment:BankCode"] ?? BankCode;
            BankAccount = _config["Payment:AccountNumber"] ?? BankAccount;
            BankAccountName = _config["Payment:AccountName"] ?? BankAccountName;

            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToPage("/Account/Login");
            }

            var currentUser = await _userRepo.GetByEmailAsync(email);
            if (currentUser == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng hiện tại.";
                return RedirectToPage("/Error");
            }

            // Lấy hợp đồng của user (đã Include Deal) để hiển thị DealName
            var allContracts = await _contractRepo.GetByUserAsync(currentUser.UserID);

            // Áp dụng bộ lọc
            var query = allContracts.AsQueryable();
            if (!string.IsNullOrWhiteSpace(StatusFilter))
                query = query.Where(c => c.ApprovalStatus == StatusFilter);
            if (FromDate.HasValue)
                query = query.Where(c => c.CreatedAt.Date >= FromDate.Value.Date);
            if (ToDate.HasValue)
                query = query.Where(c => c.CreatedAt.Date <= ToDate.Value.Date);
            if (!string.IsNullOrWhiteSpace(Keyword))
            {
                var kw = Keyword.Trim().ToLower();
                query = query.Where(c =>
                    (c.Deal != null && (c.Deal.DealName ?? string.Empty).ToLower().Contains(kw)) ||
                    (!string.IsNullOrEmpty(c.ContractContent) && c.ContractContent.ToLower().Contains(kw))
                );
            }

            Contract = query.OrderByDescending(c => c.CreatedAt).ToList();
            return Page();
        }
    }
}

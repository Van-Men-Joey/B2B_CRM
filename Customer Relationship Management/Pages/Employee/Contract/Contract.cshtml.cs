using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Repositories.Interfaces;
using Customer_Relationship_Management.Security;
using Customer_Relationship_Management.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Customer_Relationship_Management.Pages.Employee.Contract
{
    [Authorize(Roles = "Employee")]
    public class ContractModel : PageModel
    {
        private readonly IContractService _contractService;
        private readonly IDealRepository _dealRepo;
        private readonly IUserRepository _userRepo;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ContractModel> _logger;

        public ContractModel(
            IContractService contractService,
            IDealRepository dealRepo,
            IUserRepository userRepo,
            IConfiguration config,
            IWebHostEnvironment env,
            ILogger<ContractModel> logger)
        {
            _contractService = contractService;
            _dealRepo = dealRepo;
            _userRepo = userRepo;
            _config = config;
            _env = env;
            _logger = logger;
        }

        // Danh sách hợp đồng + deals cho select
        public List<Models.Contract> Contracts { get; set; } = new();
        public List<Models.Deal> Deals { get; set; } = new();

        // Bộ lọc
        [BindProperty(SupportsGet = true)] public string? StatusFilter { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? FromDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? ToDate { get; set; }
        [BindProperty(SupportsGet = true)] public string? Keyword { get; set; }

        // Cấu hình VietQR
        public string BankCode { get; set; } = "vcb";
        public string BankAccount { get; set; } = "0000000000";
        public string BankAccountName { get; set; } = "B2B CRM";

        // Add form
        [BindProperty] public AddContractInputModel AddContract { get; set; } = new();
        [BindProperty] public IFormFile? UploadFileAdd { get; set; }

        public class AddContractInputModel
        {
            [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng chọn Deal.")]
            [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn Deal.")]
            public int DealID { get; set; }

            [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Nội dung hợp đồng không được để trống.")]
            public string ContractContent { get; set; } = string.Empty;
        }

        // Edit form
        [BindProperty] public EditContractInputModel EditContract { get; set; } = new();
        [BindProperty] public IFormFile? UploadFileEdit { get; set; }

        public class EditContractInputModel
        {
            public int ContractID { get; set; }

            [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng chọn Deal.")]
            [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn Deal.")]
            public int DealID { get; set; }

            [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Nội dung hợp đồng không được để trống.")]
            public string ContractContent { get; set; } = string.Empty;
        }

        // Xử lý thanh toán
        [BindProperty] public int ContractIdToPay { get; set; }
        [BindProperty] public string PaymentMethod { get; set; } = "qr"; // qr | cash
        [BindProperty] public string? AdminUserCode { get; set; }
        [BindProperty] public string? AdminPassword { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Load cấu hình thanh toán nếu có
            BankCode = _config["Payment:BankCode"] ?? BankCode;
            BankAccount = _config["Payment:AccountNumber"] ?? BankAccount;
            BankAccountName = _config["Payment:AccountName"] ?? BankAccountName;

            var userIdClaim = User.FindFirst("UserID")?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return RedirectToPage("/Account/Login");
            if (!int.TryParse(userIdClaim, out var currentUserId)) return RedirectToPage("/Account/Login");

            // Deals của nhân viên cho tạo/sửa hợp đồng
            Deals = (await _dealRepo.GetDealsByEmployeeAsync(currentUserId)).ToList();

            // Hợp đồng của nhân viên
            var allContracts = (await _contractService.GetByUserAsync(currentUserId)).ToList();

            // Áp dụng bộ lọc (không phân biệt hoa/thường)
            var query = allContracts.AsQueryable();
            if (!string.IsNullOrWhiteSpace(StatusFilter))
                query = query.Where(c => string.Equals(c.ApprovalStatus, StatusFilter, StringComparison.OrdinalIgnoreCase));
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

            Contracts = query
                .OrderByDescending(c => c.CreatedAt)
                .ToList();

            return Page();
        }

        // JSON chi tiết hợp đồng (để xem/sửa)
        public async Task<JsonResult> OnGetContractDetailAsync(int id)
        {
            var userIdClaim = User.FindFirst("UserID")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var currentUserId))
                return new JsonResult(null);

            var c = await _contractService.GetByIdAsync(id);
            if (c == null || c.IsDeleted || c.CreatedByUserID != currentUserId) return new JsonResult(null);

            // Load Deal (nếu chưa include)
            if (c.Deal == null)
            {
                try
                {
                    var deal = (await _dealRepo.GetByIdAsync(c.DealID));
                    c.Deal = deal;
                }
                catch { }
            }

            return new JsonResult(new
            {
                c.ContractID,
                c.DealID,
                DealName = c.Deal?.DealName,
                c.ContractContent,
                c.FilePath,
                c.ApprovalStatus,
                c.PaymentStatus,
                c.PaymentAt,
                c.CreatedAt
            });
        }

        // Thêm hợp đồng (xác thực qua modal)
        public async Task<IActionResult> OnPostAddAsync()
        {
            var userIdClaim = User.FindFirst("UserID")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var currentUserId))
                return RedirectToPage("/Account/Login");

            // Chỉ validate AddContract
            ModelState.Clear();
            if (!TryValidateModel(AddContract, nameof(AddContract)))
            {
                TempData["ErrorMessage"] = string.Join("; ",
                    ModelState.Where(kvp => kvp.Key.StartsWith($"{nameof(AddContract)}."))
                              .SelectMany(v => v.Value!.Errors)
                              .Select(e => e.ErrorMessage).Distinct());
                return await OnGetAsync(); // reload lists/filters
            }

            var newContract = new Models.Contract
            {
                DealID = AddContract.DealID,
                ContractContent = AddContract.ContractContent,
                CreatedByUserID = currentUserId,
                ApprovalStatus = ContractApprovalStatus.Pending.ToString(),
                PaymentStatus = ContractPaymentStatus.Pending.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Upload file (nếu có)
            if (UploadFileAdd != null)
            {
                try
                {
                    var uploadPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "contracts");
                    if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                    var safeFileName = Path.GetFileName(UploadFileAdd.FileName);
                    var filePath = Path.Combine(uploadPath, safeFileName);
                    using var fs = new FileStream(filePath, FileMode.Create);
                    await UploadFileAdd.CopyToAsync(fs);

                    newContract.FilePath = "/uploads/contracts/" + safeFileName;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Upload file contract lỗi.");
                    TempData["ErrorMessage"] = "Không thể tải tệp hợp đồng lên.";
                    return RedirectToPage();
                }
            }

            try
            {
                await _contractService.CreateAsync(newContract, currentUserId);
                TempData["SuccessMessage"] = "Tạo hợp đồng thành công! Chờ Manager duyệt.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tạo hợp đồng.");
                TempData["ErrorMessage"] = "Lỗi khi tạo hợp đồng.";
            }

            return RedirectToPage();
        }

        // Sửa hợp đồng (chỉ khi Pending)
        public async Task<IActionResult> OnPostEditAsync()
        {
            var userIdClaim = User.FindFirst("UserID")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var currentUserId))
                return RedirectToPage("/Account/Login");

            ModelState.Clear();
            if (!TryValidateModel(EditContract, nameof(EditContract)))
            {
                TempData["ErrorMessage"] = string.Join("; ",
                    ModelState.Where(kvp => kvp.Key.StartsWith($"{nameof(EditContract)}."))
                              .SelectMany(v => v.Value!.Errors)
                              .Select(e => e.ErrorMessage).Distinct());
                return RedirectToPage();
            }

            var existing = await _contractService.GetByIdAsync(EditContract.ContractID);
            if (existing == null || existing.IsDeleted || existing.CreatedByUserID != currentUserId)
            {
                TempData["ErrorMessage"] = "Không tìm thấy hợp đồng hoặc bạn không có quyền.";
                return RedirectToPage();
            }
            if (!string.Equals(existing.ApprovalStatus, ContractApprovalStatus.Pending.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Chỉ có thể sửa hợp đồng khi trạng thái là Pending.";
                return RedirectToPage();
            }

            // Cập nhật
            existing.DealID = EditContract.DealID;
            existing.ContractContent = EditContract.ContractContent;
            existing.UpdatedAt = DateTime.UtcNow;

            // Thay tệp (nếu có)
            if (UploadFileEdit != null)
            {
                try
                {
                    var uploadPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "contracts");
                    if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                    var safeFileName = Path.GetFileName(UploadFileEdit.FileName);
                    var filePath = Path.Combine(uploadPath, safeFileName);
                    using var fs = new FileStream(filePath, FileMode.Create);
                    await UploadFileEdit.CopyToAsync(fs);

                    existing.FilePath = "/uploads/contracts/" + safeFileName;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Upload file contract (edit) lỗi.");
                    TempData["ErrorMessage"] = "Không thể tải tệp hợp đồng lên.";
                    return RedirectToPage();
                }
            }

            try
            {
                await _contractService.UpdateAsync(existing, currentUserId);
                TempData["SuccessMessage"] = "Cập nhật hợp đồng thành công.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi cập nhật hợp đồng.");
                TempData["ErrorMessage"] = "Lỗi khi cập nhật hợp đồng.";
            }

            return RedirectToPage();
        }

        // Xóa hợp đồng (Soft delete) — chỉ khi Pending
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var userIdClaim = User.FindFirst("UserID")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var currentUserId))
                return RedirectToPage("/Account/Login");

            var existing = await _contractService.GetByIdAsync(id);
            if (existing == null || existing.IsDeleted || existing.CreatedByUserID != currentUserId)
            {
                TempData["ErrorMessage"] = "Không tìm thấy hợp đồng hoặc bạn không có quyền.";
                return RedirectToPage();
            }
            if (!string.Equals(existing.ApprovalStatus, ContractApprovalStatus.Pending.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Chỉ có thể xóa hợp đồng khi trạng thái là Pending.";
                return RedirectToPage();
            }

            try
            {
                await _contractService.DeleteAsync(id, currentUserId);
                TempData["SuccessMessage"] = "Xóa hợp đồng thành công.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xóa hợp đồng.");
                TempData["ErrorMessage"] = "Lỗi khi xóa hợp đồng.";
            }

            return RedirectToPage();
        }

        // Đánh dấu thanh toán (QR hoặc Tiền mặt)
        public async Task<IActionResult> OnPostMarkPaidAsync()
        {
            var userIdClaim = User.FindFirst("UserID")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var employeeId))
                return RedirectToPage("/Account/Login");

            var contract = await _contractService.GetByIdAsync(ContractIdToPay);
            if (contract == null || contract.IsDeleted || contract.CreatedByUserID != employeeId)
            {
                TempData["ErrorMessage"] = "Không tìm thấy hợp đồng hoặc bạn không có quyền.";
                return RedirectToPage();
            }

            if (!string.Equals(contract.ApprovalStatus, ContractApprovalStatus.Approved.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Chỉ xử lý thanh toán khi hợp đồng đã được duyệt (Approved).";
                return RedirectToPage();
            }

            if (string.Equals(contract.PaymentStatus, ContractPaymentStatus.Paid.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                TempData["InfoMessage"] = "Hợp đồng đã thanh toán trước đó.";
                return RedirectToPage();
            }

            int? actorUserIdForAudit = employeeId;
            // Nếu là tiền mặt: xác thực Admin
            if (string.Equals(PaymentMethod, "cash", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(AdminUserCode) || string.IsNullOrWhiteSpace(AdminPassword))
                {
                    TempData["ErrorMessage"] = "Vui lòng nhập Mã Admin và Mật khẩu để xác nhận.";
                    return RedirectToPage();
                }

                try
                {
                    var admin = await _userRepo.GetByUserCodeAsync(AdminUserCode.Trim());
                    if (admin == null ||
                        !(admin.RoleID == 3 || string.Equals(admin.Role?.RoleName, "Admin", StringComparison.OrdinalIgnoreCase)) ||
                        !PasswordHasher.VerifyPassword(AdminPassword, admin.PasswordHash))
                    {
                        TempData["ErrorMessage"] = "Xác thực Admin không hợp lệ.";
                        return RedirectToPage();
                    }

                    actorUserIdForAudit = admin.UserID; // Audit theo Admin duyệt thanh toán tiền mặt
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi xác thực Admin cho thanh toán tiền mặt.");
                    TempData["ErrorMessage"] = "Không thể xác thực Admin.";
                    return RedirectToPage();
                }
            }

            try
            {
                contract.PaymentStatus = ContractPaymentStatus.Paid.ToString();
                contract.PaymentAt = DateTime.UtcNow;
                contract.UpdatedAt = DateTime.UtcNow;

                await _contractService.UpdateAsync(contract, actorUserIdForAudit);
                TempData["SuccessMessage"] = "Thanh toán thành công. Trạng thái đã chuyển sang 'Paid'.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi cập nhật trạng thái thanh toán.");
                TempData["ErrorMessage"] = "Không thể cập nhật trạng thái thanh toán.";
            }

            return RedirectToPage();
        }
    }
}
using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Repositories.Interfaces;
using Customer_Relationship_Management.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Customer_Relationship_Management.Pages.Employee.Deal
{
    [Authorize(Roles = "Employee")]
    public class DealModel : PageModel
    {
        private readonly IDealService _dealService;
        private readonly ICustomerRepository _customerRepository;
        private readonly ILogger<DealModel> _logger;

        public DealModel(IDealService dealService, ICustomerRepository customerRepository, ILogger<DealModel> logger)
        {
            _dealService = dealService;
            _customerRepository = customerRepository;
            _logger = logger;
        }

        public IEnumerable<Models.Deal> Deals { get; set; } = new List<Models.Deal>();
        public IEnumerable<Models.Customer> Customers { get; set; } = new List<Models.Customer>();

        // Filters
        [BindProperty(SupportsGet = true)] public string? StageFilter { get; set; }
        [BindProperty(SupportsGet = true)] public string? Keyword { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? FromDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? ToDate { get; set; }

        // Paging
        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
        public int TotalPages { get; set; }
        private const int PageSize = 8;

        // Add form model
        [BindProperty]
        public AddDealInputModel AddDeal { get; set; } = new();

        public class AddDealInputModel
        {
            [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Tên deal không được để trống.")]
            [System.ComponentModel.DataAnnotations.StringLength(200, ErrorMessage = "Tên deal không được vượt quá 200 ký tự.")]
            public string DealName { get; set; } = string.Empty;

            [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn khách hàng.")]
            public int CustomerID { get; set; }

            [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Giá trị deal là bắt buộc.")]
            [System.ComponentModel.DataAnnotations.Range(1, double.MaxValue, ErrorMessage = "Giá trị deal phải lớn hơn 0.")]
            public decimal Value { get; set; }

            public DateTime? Deadline { get; set; }
            public string? Stage { get; set; }
            public string? Notes { get; set; }
        }

        // Edit form model
        [BindProperty]
        public EditDealInputModel EditDeal { get; set; } = new();

        public class EditDealInputModel
        {
            public int DealID { get; set; }

            [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Tên deal không được để trống.")]
            public string DealName { get; set; } = string.Empty;

            [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng chọn khách hàng.")]
            public int? CustomerID { get; set; }

            [System.ComponentModel.DataAnnotations.Range(0, double.MaxValue, ErrorMessage = "Giá trị deal không hợp lệ.")]
            public decimal Value { get; set; }

            public DateTime? Deadline { get; set; }
            public string? Stage { get; set; }
            public string? Notes { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdClaim = User.FindFirst("UserID")?.Value;
            if (userIdClaim == null) return RedirectToPage("/Account/Login");
            int employeeId = int.Parse(userIdClaim);

            Customers = await _customerRepository.GetByAssignedUserAsync(employeeId);

            IEnumerable<Models.Deal> allDeals;
            if (!string.IsNullOrEmpty(Keyword))
                allDeals = await _dealService.SearchDealsAsync(employeeId, Keyword);
            else if (!string.IsNullOrEmpty(StageFilter))
                allDeals = await _dealService.GetDealsByStageAsync(StageFilter, employeeId);
            else
                allDeals = await _dealService.GetDealsByEmployeeAsync(employeeId);

            if (FromDate.HasValue) allDeals = allDeals.Where(d => d.Deadline >= FromDate.Value);
            if (ToDate.HasValue) allDeals = allDeals.Where(d => d.Deadline <= ToDate.Value);

            int totalDeals = allDeals.Count();
            TotalPages = (int)Math.Ceiling(totalDeals / (double)PageSize);
            PageNumber = Math.Max(1, Math.Min(PageNumber, TotalPages == 0 ? 1 : TotalPages));

            Deals = allDeals
                .OrderByDescending(d => d.CreatedAt)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            return Page();
        }

        // JSON chi tiết deal
        public async Task<JsonResult> OnGetDealDetailAsync(int id)
        {
            var userIdClaim = User.FindFirst("UserID")?.Value;
            if (userIdClaim == null) return new JsonResult(null);
            int employeeId = int.Parse(userIdClaim);

            var deal = await _dealService.GetDealByIdAsync(id, employeeId);
            return new JsonResult(deal);
        }

        // Add — chỉ validate AddDeal
        public async Task<IActionResult> OnPostAddAsync()
        {
            var userIdClaim = User.FindFirst("UserID")?.Value;
            if (userIdClaim == null) return RedirectToPage("/Account/Login");
            int employeeId = int.Parse(userIdClaim);

            // Chỉ validate model AddDeal
            ModelState.Clear();
            if (!TryValidateModel(AddDeal, nameof(AddDeal)))
            {
                Customers = await _customerRepository.GetByAssignedUserAsync(employeeId);
                TempData["ErrorMessage"] = string.Join("; ",
                    ModelState.Where(kvp => kvp.Key.StartsWith($"{nameof(AddDeal)}."))
                              .SelectMany(v => v.Value!.Errors)
                              .Select(e => e.ErrorMessage).Distinct());
                return await OnGetAsync();
            }

            var deal = new Models.Deal
            {
                DealName = AddDeal.DealName,
                CustomerID = AddDeal.CustomerID,
                Value = AddDeal.Value,
                Deadline = AddDeal.Deadline,
                Stage = string.IsNullOrWhiteSpace(AddDeal.Stage) ? "Lead" : AddDeal.Stage!,
                Notes = AddDeal.Notes,
                CreatedByUserID = employeeId
            };

            var (success, message) = await _dealService.CreateDealAsync(deal);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToPage();
        }

        // Edit — chỉ validate EditDeal
        public async Task<IActionResult> OnPostEditAsync()
        {
            var userIdClaim = User.FindFirst("UserID")?.Value;
            if (userIdClaim == null) return RedirectToPage("/Account/Login");
            int employeeId = int.Parse(userIdClaim);

            ModelState.Clear();
            if (!TryValidateModel(EditDeal, nameof(EditDeal)))
            {
                TempData["ErrorMessage"] = string.Join("; ",
                    ModelState.Where(kvp => kvp.Key.StartsWith($"{nameof(EditDeal)}."))
                              .SelectMany(v => v.Value!.Errors)
                              .Select(e => e.ErrorMessage).Distinct());
                return RedirectToPage();
            }

            if (!EditDeal.CustomerID.HasValue)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn khách hàng.";
                return RedirectToPage();
            }

            var current = await _dealService.GetDealByIdAsync(EditDeal.DealID, employeeId);
            if (current == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy Deal hoặc bạn không có quyền chỉnh sửa.";
                return RedirectToPage();
            }

            bool changed =
                current.DealName != EditDeal.DealName ||
                current.CustomerID != EditDeal.CustomerID.Value ||
                current.Value != EditDeal.Value ||
                current.Deadline != EditDeal.Deadline ||
                current.Stage != EditDeal.Stage ||
                current.Notes != EditDeal.Notes;

            if (!changed)
            {
                TempData["InfoMessage"] = "Không có thay đổi nào để cập nhật.";
                return RedirectToPage();
            }

            var updated = new Models.Deal
            {
                DealID = EditDeal.DealID,
                DealName = EditDeal.DealName,
                CustomerID = EditDeal.CustomerID.Value,
                Value = EditDeal.Value,
                Deadline = EditDeal.Deadline,
                Stage = EditDeal.Stage ?? current.Stage,
                Notes = EditDeal.Notes
            };

            var (success, message) = await _dealService.UpdateDealAsync(updated, employeeId);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;

            return RedirectToPage();
        }

        // Delete
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var userIdClaim = User.FindFirst("UserID")?.Value;
            if (userIdClaim == null) return RedirectToPage("/Account/Login");
            int employeeId = int.Parse(userIdClaim);

            var (success, message) = await _dealService.SoftDeleteAsync(id, employeeId);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToPage();
        }

        // Update stage nhanh
        [BindProperty] public int DealIdToUpdate { get; set; }
        [BindProperty] public string? NewStage { get; set; }

        public async Task<IActionResult> OnPostUpdateStageAsync()
        {
            var userIdClaim = User.FindFirst("UserID")?.Value;
            if (userIdClaim == null) return RedirectToPage("/Account/Login");
            int employeeId = int.Parse(userIdClaim);

            if (DealIdToUpdate <= 0 || string.IsNullOrEmpty(NewStage))
            {
                TempData["ErrorMessage"] = "Dữ liệu cập nhật không hợp lệ.";
                return RedirectToPage();
            }

            try
            {
                bool result = await _dealService.UpdateDealStageAsync(DealIdToUpdate, NewStage!, employeeId);
                TempData[result ? "SuccessMessage" : "ErrorMessage"] =
                    result ? "✅ Cập nhật giai đoạn Deal thành công." : "❌ Không thể cập nhật giai đoạn Deal.";
            }
            catch
            {
                TempData["ErrorMessage"] = "❌ Đã xảy ra lỗi trong quá trình cập nhật.";
            }

            return RedirectToPage();
        }
    }
}
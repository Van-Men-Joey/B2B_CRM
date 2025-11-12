using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Customer_Relationship_Management.Pages.Employee.Customer
{
    public class CustomerModel : PageModel
    {
        private readonly ICustomerService _customerService;
        private const int PageSize = 10;

        public CustomerModel(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        public IEnumerable<Models.Customer> Customers { get; set; } = new List<Models.Customer>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        [BindProperty]
        public CustomerInputModel NewCustomer { get; set; } = new();

        [BindProperty]
        public EditCustomerInputModel EditCustomer { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public FilterModel Filter { get; set; } = new();

        public class FilterModel
        {
            public string? Keyword { get; set; }
            public string? Industry { get; set; }
            public string? Scale { get; set; }
            public string? Address { get; set; }
            public bool? VIP { get; set; }
            public string? DateFilter { get; set; } // today, week, month, year
        }

        public class CustomerInputModel
        {
            [Required(ErrorMessage = "Tên công ty không được để trống.")]
            [StringLength(100, ErrorMessage = "Tên công ty tối đa 100 ký tự.")]
            public string CompanyName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Tên người liên hệ không được để trống.")]
            [StringLength(100, ErrorMessage = "Tên người liên hệ tối đa 100 ký tự.")]
            public string ContactName { get; set; } = string.Empty;

            [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
            public string? ContactEmail { get; set; }

            [Required(ErrorMessage = "Vui lòng nhập SĐT.")]
            [RegularExpression(@"^\d{9,15}$", ErrorMessage = "Số điện thoại chỉ chứa 9–15 chữ số.")]
            public string? ContactPhone { get; set; }

            public string? Industry { get; set; }
            public string? Scale { get; set; }
            public string? Address { get; set; }
            public string? ContactTitle { get; set; }
            public string? Notes { get; set; }
        }

        public class EditCustomerInputModel
        {
            public int CustomerID { get; set; }
            public string CustomerCode { get; set; } = string.Empty;

            [Required(ErrorMessage = "Tên công ty không được để trống.")]
            [StringLength(100, ErrorMessage = "Tên công ty tối đa 100 ký tự.")]
            public string CompanyName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Tên người liên hệ không được để trống.")]
            [StringLength(100, ErrorMessage = "Tên người liên hệ tối đa 100 ký tự.")]
            public string ContactName { get; set; } = string.Empty;

            [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
            public string? ContactEmail { get; set; }

            [Required(ErrorMessage = "Vui lòng nhập SĐT.")]
            [RegularExpression(@"^\d{9,15}$", ErrorMessage = "Số điện thoại chỉ chứa 9–15 chữ số.")]
            public string? ContactPhone { get; set; }

            public string? Industry { get; set; }
            public string? Scale { get; set; }
            public string? Address { get; set; }
            public string? ContactTitle { get; set; }
            public string? Notes { get; set; }

            public bool VIP { get; set; }
        }

        // GET – danh sách + bộ lọc + tìm kiếm + phân trang
        public async Task<IActionResult> OnGetAsync(int pageNumber = 1)
        {
            var claim = User.FindFirst("UserID");
            if (claim == null) return RedirectToPage("/Account/Login");

            int userId = int.Parse(claim.Value);
            var allCustomers = await _customerService.GetCustomersForUserAsync(userId);

            // Lọc
            if (!string.IsNullOrWhiteSpace(Filter.Keyword))
            {
                string keyword = Filter.Keyword.ToLower();
                allCustomers = allCustomers.Where(c =>
                    (c.CustomerCode ?? "").ToLower().Contains(keyword) ||
                    (c.CompanyName ?? "").ToLower().Contains(keyword) ||
                    (c.ContactName ?? "").ToLower().Contains(keyword) ||
                    (c.ContactEmail ?? "").ToLower().Contains(keyword) ||
                    (c.ContactPhone ?? "").ToLower().Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(Filter.Industry))
                allCustomers = allCustomers.Where(c => (c.Industry ?? "").Contains(Filter.Industry, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(Filter.Scale))
                allCustomers = allCustomers.Where(c => (c.Scale ?? "").Contains(Filter.Scale, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(Filter.Address))
                allCustomers = allCustomers.Where(c => (c.Address ?? "").Contains(Filter.Address, StringComparison.OrdinalIgnoreCase));

            if (Filter.VIP.HasValue)
                allCustomers = allCustomers.Where(c => c.VIP == Filter.VIP.Value);

            if (!string.IsNullOrEmpty(Filter.DateFilter))
            {
                DateTime now = DateTime.Now;
                allCustomers = Filter.DateFilter switch
                {
                    "today" => allCustomers.Where(c => c.CreatedAt.Date == now.Date),
                    "week" => allCustomers.Where(c => c.CreatedAt >= now.AddDays(-7)),
                    "month" => allCustomers.Where(c => c.CreatedAt.Month == now.Month && c.CreatedAt.Year == now.Year),
                    "year" => allCustomers.Where(c => c.CreatedAt.Year == now.Year),
                    _ => allCustomers
                };
            }

            // Phân trang
            int totalCount = allCustomers.Count();
            TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
            CurrentPage = Math.Max(1, Math.Min(pageNumber, TotalPages == 0 ? 1 : TotalPages));

            Customers = allCustomers
                .OrderByDescending(c => c.CreatedAt)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            return Page();
        }

        // GET – chi tiết (JSON) dùng cho modal xem + điền form sửa
        public async Task<JsonResult> OnGetCustomerDetailAsync(int id)
        {
            var claim = User.FindFirst("UserID");
            if (claim == null) return new JsonResult(null);

            int userId = int.Parse(claim.Value);
            var customer = await _customerService.GetCustomerByCustomerID_UserIDAsync(id, userId);
            return new JsonResult(customer);
        }

        // Helper gom lỗi theo prefix
        private static string CollectErrorsForPrefix(ModelStateDictionary modelState, string prefix)
        {
            return string.Join("; ",
                modelState.Where(kvp => kvp.Key.StartsWith(prefix + ".", StringComparison.OrdinalIgnoreCase))
                          .SelectMany(v => v.Value!.Errors)
                          .Select(e => e.ErrorMessage)
                          .Distinct());
        }

        // POST – thêm (chỉ validate NewCustomer)
        public async Task<IActionResult> OnPostAddAsync(int pageNumber = 1)
        {
            var claim = User.FindFirst("UserID");
            if (claim == null) return RedirectToPage("/Account/Login");

            int userId = int.Parse(claim.Value);

            // Chỉ validate đúng prefix NewCustomer
            ModelState.Clear();
            if (!TryValidateModel(NewCustomer, nameof(NewCustomer)))
            {
                TempData["Error"] = CollectErrorsForPrefix(ModelState, nameof(NewCustomer));
                return await OnGetAsync(pageNumber);
            }

            var customer = new Models.Customer
            {
                CompanyName = NewCustomer.CompanyName?.Trim() ?? string.Empty,
                ContactName = NewCustomer.ContactName?.Trim() ?? string.Empty,
                ContactEmail = string.IsNullOrWhiteSpace(NewCustomer.ContactEmail) ? null : NewCustomer.ContactEmail.Trim(),
                ContactPhone = string.IsNullOrWhiteSpace(NewCustomer.ContactPhone) ? null : NewCustomer.ContactPhone.Trim(),
                Industry = string.IsNullOrWhiteSpace(NewCustomer.Industry) ? null : NewCustomer.Industry.Trim(),
                Scale = string.IsNullOrWhiteSpace(NewCustomer.Scale) ? null : NewCustomer.Scale.Trim(),
                Address = string.IsNullOrWhiteSpace(NewCustomer.Address) ? null : NewCustomer.Address.Trim(),
                ContactTitle = string.IsNullOrWhiteSpace(NewCustomer.ContactTitle) ? null : NewCustomer.ContactTitle.Trim(),
                Notes = string.IsNullOrWhiteSpace(NewCustomer.Notes) ? null : NewCustomer.Notes.Trim()
            };

            var result = await _customerService.AddCustomerAsync(customer, userId);
            TempData[result.Success ? "Success" : "Error"] = result.Message;

            return RedirectToPage(new { pageNumber });
        }

        // POST – sửa (chỉ validate EditCustomer)
        public async Task<IActionResult> OnPostEditAsync(int pageNumber = 1)
        {
            var claim = User.FindFirst("UserID");
            if (claim == null) return RedirectToPage("/Account/Login");

            int userId = int.Parse(claim.Value);

            // Chỉ validate đúng prefix EditCustomer
            ModelState.Clear();
            if (!TryValidateModel(EditCustomer, nameof(EditCustomer)))
            {
                TempData["Error"] = CollectErrorsForPrefix(ModelState, nameof(EditCustomer));
                return RedirectToPage(new { pageNumber });
            }

            var customer = await _customerService.GetCustomerByCustomerID_UserIDAsync(EditCustomer.CustomerID, userId);
            if (customer == null)
            {
                TempData["Error"] = "Khách hàng không tồn tại hoặc bạn không có quyền chỉnh sửa.";
                return RedirectToPage(new { pageNumber });
            }

            bool isChanged =
                customer.CompanyName != EditCustomer.CompanyName ||
                customer.ContactName != EditCustomer.ContactName ||
                customer.ContactEmail != EditCustomer.ContactEmail ||
                customer.ContactPhone != EditCustomer.ContactPhone ||
                customer.Industry != EditCustomer.Industry ||
                customer.Scale != EditCustomer.Scale ||
                customer.Address != EditCustomer.Address ||
                customer.ContactTitle != EditCustomer.ContactTitle ||
                customer.Notes != EditCustomer.Notes;

            if (!isChanged)
            {
                TempData["Info"] = "Không có thay đổi nào để cập nhật.";
                return RedirectToPage(new { pageNumber });
            }

            customer.CompanyName = EditCustomer.CompanyName.Trim();
            customer.ContactName = EditCustomer.ContactName.Trim();
            customer.ContactEmail = string.IsNullOrWhiteSpace(EditCustomer.ContactEmail) ? null : EditCustomer.ContactEmail.Trim();
            customer.ContactPhone = string.IsNullOrWhiteSpace(EditCustomer.ContactPhone) ? null : EditCustomer.ContactPhone.Trim();
            customer.Industry = string.IsNullOrWhiteSpace(EditCustomer.Industry) ? null : EditCustomer.Industry.Trim();
            customer.Scale = string.IsNullOrWhiteSpace(EditCustomer.Scale) ? null : EditCustomer.Scale.Trim();
            customer.Address = string.IsNullOrWhiteSpace(EditCustomer.Address) ? null : EditCustomer.Address.Trim();
            customer.ContactTitle = string.IsNullOrWhiteSpace(EditCustomer.ContactTitle) ? null : EditCustomer.ContactTitle.Trim();
            customer.Notes = string.IsNullOrWhiteSpace(EditCustomer.Notes) ? null : EditCustomer.Notes.Trim();

            bool updated = await _customerService.UpdateCustomerAsync(customer, userId);
            TempData[updated ? "Success" : "Error"] = updated
                ? "Cập nhật thông tin khách hàng thành công!"
                : "Không thể cập nhật khách hàng.";

            return RedirectToPage(new { pageNumber });
        }

        // POST – xóa
        public async Task<IActionResult> OnPostDeleteAsync(int id, int pageNumber = 1)
        {
            var claim = User.FindFirst("UserID");
            if (claim == null) return RedirectToPage("/Account/Login");

            int userId = int.Parse(claim.Value);
            bool deleted = await _customerService.DeleteCustomerAsync(id, userId);

            TempData[deleted ? "Success" : "Error"] = deleted
                ? "Xóa khách hàng thành công!"
                : "Không thể xóa khách hàng này.";

            return RedirectToPage(new { pageNumber });
        }
    }
}
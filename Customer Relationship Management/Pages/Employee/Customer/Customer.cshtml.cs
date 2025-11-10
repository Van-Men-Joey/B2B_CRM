using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Customer_Relationship_Management.Pages.Employee.Customer
{
    public class CustomerModel : PageModel
    {
        private readonly ICustomerService _customerService;
        private const int PageSize = 10; // mỗi trang hiển thị 10 KH

        public CustomerModel(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        public IEnumerable<Models.Customer> Customers { get; set; } = new List<Models.Customer>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        [BindProperty]
        public CustomerInputModel NewCustomer { get; set; } = new CustomerInputModel();
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
        // ✅ GET – danh sách + bộ lọc + tìm kiếm + phân trang
        public async Task<IActionResult> OnGetAsync(int pageNumber = 1)
        {
            var claim = User.FindFirst("UserID");
            if (claim == null)
                return RedirectToPage("/Account/Login");

            int userId = int.Parse(claim.Value);
            var allCustomers = await _customerService.GetCustomersForUserAsync(userId);

            // --- Lọc dữ liệu ---
            if (!string.IsNullOrWhiteSpace(Filter.Keyword))
            {
                string keyword = Filter.Keyword.ToLower();
                allCustomers = allCustomers.Where(c =>
                    (c.CustomerCode ?? "").ToLower().Contains(keyword) ||
                    ( c.CompanyName ?? "").ToLower().Contains(keyword) ||
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

            // --- Lọc theo ngày ---
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

            // --- Phân trang ---
            int totalCount = allCustomers.Count();
            TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
            CurrentPage = pageNumber;

            Customers = allCustomers
                .OrderByDescending(c => c.CreatedAt)
                .Skip((pageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            return Page();
        }

        // GET – lấy chi tiết khách hàng (cho modal)
        public async Task<JsonResult> OnGetCustomerDetailAsync(int id)
        {
            var claim = User.FindFirst("UserID");
            if (claim == null)
                return new JsonResult(null);

            int userId = int.Parse(claim.Value);
            var customer = await _customerService.GetCustomerByCustomerID_UserIDAsync(id, userId);
            //chỉ UserID quản lí Customer đó mới xem được của người đó
            return new JsonResult(customer);
        }

        // POST – thêm khách hàng (kèm kiểm tra hợp lệ)
        public async Task<IActionResult> OnPostAddAsync()
        {
            var claim = User.FindFirst("UserID");
            if (claim == null)
                return RedirectToPage("/Account/Login");

            int userId = int.Parse(claim.Value);

            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join("; ",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return await OnGetAsync();
            }

            var customer = new Models.Customer
            {
                CompanyName = NewCustomer.CompanyName,
                ContactName = NewCustomer.ContactName,
                ContactEmail = NewCustomer.ContactEmail,
                ContactPhone = NewCustomer.ContactPhone,
                Industry = NewCustomer.Industry,
                Scale = NewCustomer.Scale,
                Address = NewCustomer.Address,
                ContactTitle = NewCustomer.ContactTitle,
                Notes = NewCustomer.Notes
            };

            // ✅ Gọi service mới có tuple trả về
            var result = await _customerService.AddCustomerAsync(customer, userId);

            if (result.Success)
                TempData["Success"] = result.Message;
            else
                TempData["Error"] = result.Message;

            return RedirectToPage(new { pageNumber = CurrentPage });
        }


        // POST – xóa khách hàng
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var claim = User.FindFirst("UserID");
            if (claim == null)
                return RedirectToPage("/Account/Login");

            int userId = int.Parse(claim.Value);
            bool deleted = await _customerService.DeleteCustomerAsync(id, userId);
            TempData["Message"] = deleted
                ? "Xóa khách hàng thành công!"
                : "Không thể xóa khách hàng này.";

            return RedirectToPage(new { pageNumber = CurrentPage });
        }
    }
}

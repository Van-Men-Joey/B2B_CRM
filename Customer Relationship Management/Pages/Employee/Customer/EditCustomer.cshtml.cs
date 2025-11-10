using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Customer_Relationship_Management.Pages.Employee.Customer
{
    public class EditCustomerModel : PageModel
    {
        private readonly ICustomerService _customerService;

        public EditCustomerModel(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        [BindProperty]
        public EditCustomerInputModel EditCustomer { get; set; } = new();

        public class EditCustomerInputModel
        {
            public int CustomerID { get; set; }
            [BindNever]
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

            public bool VIP { get; set; } // không cho edit
        }

        // GET – load dữ liệu
        public async Task<IActionResult> OnGetAsync(int id)
        {
            var claim = User.FindFirst("UserID");
            if (claim == null)
                return RedirectToPage("/Account/Login");

            int userId = int.Parse(claim.Value);

            var customer = await _customerService.GetCustomerByCustomerID_UserIDAsync(id, userId);
            if (customer == null)
            {
                TempData["Error"] = "Khách hàng không tồn tại hoặc bạn không có quyền chỉnh sửa.";
                return RedirectToPage("/Employee/Customer/Customer");
            }

            EditCustomer = new EditCustomerInputModel
            {
                CustomerID = customer.CustomerID,
                CustomerCode = customer.CustomerCode,
                CompanyName = customer.CompanyName,
                ContactName = customer.ContactName,
                ContactEmail = customer.ContactEmail,
                ContactPhone = customer.ContactPhone,
                Industry = customer.Industry,
                Scale = customer.Scale,
                Address = customer.Address,
                ContactTitle = customer.ContactTitle,
                Notes = customer.Notes,
                VIP = customer.VIP
            };

            return Page();
        }

        // POST – cập nhật thông tin
        public async Task<IActionResult> OnPostAsync()
        {
            var claim = User.FindFirst("UserID");
            if (claim == null)
                return RedirectToPage("/Account/Login");

            int userId = int.Parse(claim.Value);

            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join("; ",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Page();
            }

            // kiểm tra quyền sở hữu và lấy dữ liệu gốc
            var customer = await _customerService.GetCustomerByCustomerID_UserIDAsync(EditCustomer.CustomerID, userId);
            if (customer == null)
            {
                TempData["Error"] = "Khách hàng không tồn tại hoặc bạn không có quyền chỉnh sửa.";
                return RedirectToPage("/Employee/Customer/Customer");
            }

            // Kiểm tra xem dữ liệu có thay đổi không
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
                return RedirectToPage("/Employee/Customer/Customer");
            }

            // Cập nhật các trường đã thay đổi
            customer.CompanyName = EditCustomer.CompanyName;
            customer.ContactName = EditCustomer.ContactName;
            customer.ContactEmail = EditCustomer.ContactEmail;
            customer.ContactPhone = EditCustomer.ContactPhone;
            customer.Industry = EditCustomer.Industry;
            customer.Scale = EditCustomer.Scale;
            customer.Address = EditCustomer.Address;
            customer.ContactTitle = EditCustomer.ContactTitle;
            customer.Notes = EditCustomer.Notes;

            bool updated = await _customerService.UpdateCustomerAsync(customer, userId);

            TempData[updated ? "Success" : "Error"] = updated
                ? "Cập nhật thông tin khách hàng thành công!"
                : "Không thể cập nhật khách hàng.";

            return RedirectToPage("/Employee/Customer/Customer");
        }
    }
}

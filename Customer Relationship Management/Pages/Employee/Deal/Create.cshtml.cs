using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Repositories.Interfaces;
using Customer_Relationship_Management.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Customer_Relationship_Management.Pages.Employee.Deal
{
    [Authorize(Roles = "Employee")]
    public class CreateModel : PageModel
    {
        private readonly IDealService _dealService;
        private readonly ICustomerRepository _customerRepository;

        public CreateModel(IDealService dealService, ICustomerRepository customerRepository)
        {
            _dealService = dealService;
            _customerRepository = customerRepository;
        }

        [BindProperty]
        public Models.Deal NewDeal { get; set; } = new Models.Deal();

        public IEnumerable<Models.Customer>? Customers { get; set; }

        // Load danh sách khách hàng khi mở trang
        public async Task<IActionResult> OnGetAsync()
        {
            // Lấy ID của nhân viên đăng nhập
            var userIdClaim = User.FindFirst("UserID")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return RedirectToPage("/Account/Login");

            int employeeId = int.Parse(userIdClaim);

            // Chỉ lấy khách hàng có AssignedToUserID = employeeId
            Customers = await _customerRepository.GetByAssignedUserAsync(employeeId);

            return Page();
        }


        // Xử lý khi người dùng nhấn nút "Tạo deal"
        public async Task<IActionResult> OnPostAsync()
        {
            Console.WriteLine("DEBUG => OnPostAsync called");
            Console.WriteLine($"DealName: {NewDeal.DealName}, CustomerID: {NewDeal.CustomerID}, Value: {NewDeal.Value}");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("DEBUG => ModelState invalid");
                Customers = await _customerRepository.GetAllAsync();
                return Page();
            }

            // Lấy ID của nhân viên đăng nhập
            var userIdClaim = User.FindFirst("UserID")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                Console.WriteLine("DEBUG => Không tìm thấy UserID trong Claims");
                return RedirectToPage("/Account/Login");
            }

            int employeeId = int.Parse(userIdClaim);

            // Gán các giá trị mặc định
            NewDeal.CreatedByUserID = employeeId;
            NewDeal.CreatedAt = DateTime.UtcNow;
            NewDeal.UpdatedAt = DateTime.UtcNow;
            if (string.IsNullOrEmpty(NewDeal.Stage))
                NewDeal.Stage = "Lead";
            NewDeal.IsDeleted = false;

            Console.WriteLine("DEBUG => Gọi CreateDealAsync...");
            var (success, message) = await _dealService.CreateDealAsync(NewDeal);
            Console.WriteLine($"DEBUG => Kết quả CreateDealAsync: {success}, Message: {message}");

            if (success)
            {
                TempData["SuccessMessage"] = message;
                return RedirectToPage("/Employee/Deal/Index");
            }
            else
            {
                TempData["ErrorMessage"] = message;
                Customers = await _customerRepository.GetAllAsync();
                return Page();
            }
        }
    }
}

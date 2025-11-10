using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Customer_Relationship_Management.Controllers.Employee
{
    [Route("api/employee/customer")]
    [ApiController]
    [Authorize] // bảo vệ chỉ người đăng nhập được truy cập
    public class CustomerApiController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        public CustomerApiController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        /// <summary>
        /// API lấy chi tiết khách hàng theo ID (và xác minh quyền sở hữu)
        /// </summary>
        [HttpGet("detail/{id}")]
        public async Task<IActionResult> GetCustomerDetail(int id)
        {
            var claim = User.FindFirst("UserID");
            if (claim == null)
            {
                Console.WriteLine("⚠️ Không có claim UserID trong token!");
                return Unauthorized(new { message = "Bạn chưa đăng nhập." });
            }

            int userId = int.Parse(claim.Value);
            Console.WriteLine($"📩 Request xem chi tiết CustomerID={id} bởi UserID={userId}");

            var customer = await _customerService.GetCustomerByCustomerID_UserIDAsync(id, userId);

            if (customer == null)
            {
                Console.WriteLine($"❌ Không tìm thấy CustomerID={id} cho UserID={userId}");
                return NotFound(new { message = "Không tìm thấy khách hàng hoặc bạn không có quyền xem." });
            }

            Console.WriteLine($"✅ Tìm thấy khách hàng: {customer.CompanyName}");

            return Ok(new
            {
                customer.CustomerID,
                customer.CustomerCode,
                customer.CompanyName,
                customer.ContactName,
                customer.ContactEmail,
                customer.ContactPhone,
                customer.Industry,
                customer.Scale,
                customer.Address,
                customer.Notes,
                customer.VIP,
                customer.CreatedAt,
                customer.UpdatedAt
            });
        }
    }
}

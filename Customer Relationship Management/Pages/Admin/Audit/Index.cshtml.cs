using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace Customer_Relationship_Management.Pages.Admin.Audit
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        public Dictionary<string, string> AvailableLogs { get; set; }

        public void OnGet()
        {
            // Giữ lại 4 loại Audit Log cần thiết
            AvailableLogs = new Dictionary<string, string>
            {
                { "Users", "Quản lý tài khoản, đăng nhập, reset, phân quyền." },
                { "Customers", "Nhật ký thay đổi thông tin khách hàng." },
                { "Deals", "Hoạt động liên quan đến deals, trạng thái." },
                { "Contracts", "Quản lý và duyệt hợp đồng, nhật ký." }
            };
        }
    }
}
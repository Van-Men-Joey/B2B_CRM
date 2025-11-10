using Customer_Relationship_Management.ViewModels.Customer;
using Customer_Relationship_Management.ViewModels.SupportTicket;

namespace Customer_Relationship_Management.ViewModels.User
{
    public class AdminViewModel
    {
        public Guid AdminId { get; set; }      // hoặc int nếu bạn dùng Identity
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }

        // Những thông tin tổng quan admin có thể cần
        public int TotalUsers { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalDeals { get; set; }
        public int TotalSupportTickets { get; set; }

        // Có thể thêm danh sách nếu muốn hiển thị trực tiếp
        public IEnumerable<UserViewModel> Users { get; set; }
        public IEnumerable<CustomerViewModel> Customers { get; set; }
        public IEnumerable<SupportTicketViewModel> Tickets { get; set; }
    }
}

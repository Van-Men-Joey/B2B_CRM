namespace Customer_Relationship_Management.ViewModels.Customer
{
    public class CustomerViewModel
    {
        public int? CustomerID { get; set; }  // null khi tạo mới
        public string? CustomerCode { get; set; }
        public string CompanyName { get; set; } = null!;
        public string? Industry { get; set; }
        public string? Scale { get; set; }
        public string? Address { get; set; }

        // Contact info
        public string ContactName { get; set; } = null!;
        public string? ContactTitle { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }

        public bool VIP { get; set; }
        public int? AssignedToUserID { get; set; }
        public string? Notes { get; set; }

        // Dùng cho dropdown list user trong UI
        public string? AssignedToUserName { get; set; }
    }
}

namespace Customer_Relationship_Management.ViewModels.Customer
{
    public class CreateCustomerViewModel
    {
        public string CompanyName { get; set; } = null!;
        public string? Industry { get; set; }
        public string? Scale { get; set; }
        public string? Address { get; set; }
        public string ContactName { get; set; } = null!;
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public bool VIP { get; set; }
        public int? AssignedToUserID { get; set; }
    }
}

namespace Customer_Relationship_Management.ViewModels.Deal
{
    public class CreateDealViewModel
    {
        public int CustomerID { get; set; }
        public string? DealName { get; set; }
        public decimal Value { get; set; }
        public DateTime? Deadline { get; set; }
    }
}

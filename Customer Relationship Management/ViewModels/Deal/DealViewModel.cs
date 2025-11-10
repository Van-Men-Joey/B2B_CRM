namespace Customer_Relationship_Management.ViewModels.Deal
{
    public class DealViewModel
    {
        public int? DealID { get; set; }
        public int CustomerID { get; set; }
        public string CustomerName { get; set; } = null!;

        public string? DealName { get; set; }
        public decimal Value { get; set; }
        public string Stage { get; set; } = "Lead"; // Lead, Negotiation...
        public DateTime? Deadline { get; set; }

        public string? Notes { get; set; }
        public int CreatedByUserID { get; set; }
        public string? CreatedByUserName { get; set; }

        // upload file tạm thời lưu ở UI (nếu có)
        public IFormFile? Attachment { get; set; }
        public string? AttachmentPath { get; set; }
    }
}

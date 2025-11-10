namespace Customer_Relationship_Management.ViewModels.Contract
{
    public class ContractViewModel
    {
        public int? ContractID { get; set; }
        public int DealID { get; set; }
        public string DealName { get; set; } = null!;

        public string ContractContent { get; set; } = null!;
        public IFormFile? FileUpload { get; set; }
        public string? FilePath { get; set; }
        public string? FileHash { get; set; }

        public string ApprovalStatus { get; set; } = "Pending"; // Pending, Approved
        public int CreatedByUserID { get; set; }

        public string? QRCodeLink { get; set; }
        public string PaymentStatus { get; set; } = "Pending"; // Pending, Paid
        public DateTime? PaymentAt { get; set; }

        public bool IsSensitive { get; set; }
    }
}

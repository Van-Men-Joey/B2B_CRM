namespace Customer_Relationship_Management.ViewModels
{
    public class WebhookDto
    {
        public string Gateway { get; set; } = string.Empty; // Casso, SePay...
        public string TransactionDate { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string SubAccount { get; set; } = string.Empty; // Số tài khoản phụ (nếu có)
        public decimal Amount { get; set; } // Số tiền nhận
        public string Content { get; set; } = string.Empty; // Nội dung chuyển khoản (Quan trọng nhất)
        public string TransferType { get; set; } = string.Empty; // in/out
        public string TransactionId { get; set; } = string.Empty; // Mã tham chiếu ngân hàng
    }
}
namespace Customer_Relationship_Management.ViewModels.SupportTicket
{
    public class SupportTicketViewModel
    {
        public Guid? TicketID { get; set; }
        public int CustomerID { get; set; }
        public string CustomerName { get; set; } = null!;

        public string Title { get; set; } = null!;
        public string? Description { get; set; }

        public string Status { get; set; } = "Open"; // Open, InProgress, Closed
        public string Priority { get; set; } = "Normal"; // Low, Normal, High

        public int CreatedByUserID { get; set; }
        public int? AssignedToUserID { get; set; }
    }
}

namespace Customer_Relationship_Management.ViewModels.SupportTicket
{
    public class CreateSupportTicketViewModel
    {
        public int CustomerID { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string Priority { get; set; } = "Normal";
        public int? AssignedToUserID { get; set; }
    }
}

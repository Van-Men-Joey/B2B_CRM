namespace Customer_Relationship_Management.ViewModels.Task
{
    public class CreateTaskViewModel
    {
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public int AssignedToUserID { get; set; }
        public int? RelatedDealID { get; set; }
        public DateTime? DueDate { get; set; }
    }
}

namespace Customer_Relationship_Management.ViewModels.Task
{
    public class TaskViewModel
    {
        public int? TaskID { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }

        public int AssignedToUserID { get; set; }
        public int CreatedByUserID { get; set; }

        public int? RelatedDealID { get; set; }
        public DateTime? DueDate { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, In-progress, Done
        public DateTime? ReminderAt { get; set; }
    }
}

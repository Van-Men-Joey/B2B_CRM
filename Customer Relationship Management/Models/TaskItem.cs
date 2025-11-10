using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Customer_Relationship_Management.Models
{
    public class TaskItem
    {
        [Key]
        public int TaskID { get; set; }

        public string Title { get; set; } = null!;
        public string? Description { get; set; }

        public int AssignedToUserID { get; set; }
        public int CreatedByUserID { get; set; }
        public int? RelatedDealID { get; set; }

        public DateTime? DueDate { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime? ReminderAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation
        [ForeignKey("AssignedToUserID")]
        public User AssignedToUser { get; set; } = null!;
        public User CreatedByUser { get; set; } = null!;
        public Deal? RelatedDeal { get; set; }
    }
}

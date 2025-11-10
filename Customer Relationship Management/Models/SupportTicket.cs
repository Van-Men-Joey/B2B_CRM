using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Customer_Relationship_Management.Models
{
    public class SupportTicket
    {
        [Key]
        public Guid TicketID { get; set; }
        public int CustomerID { get; set; }
        public int CreatedByUserID { get; set; }
        public int? AssignedToUserID { get; set; }

        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string Status { get; set; } = "Open";
        public string Priority { get; set; } = "Normal";
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation
        public Customer Customer { get; set; } = null!;
        public User CreatedBy { get; set; } = null!;
        [ForeignKey("AssignedToUserID")]
        public User? AssignedToUser { get; set; }
    }
}

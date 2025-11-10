using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Customer_Relationship_Management.Repositories.Interfaces;

namespace Customer_Relationship_Management.Models
{
    public class Contract : IEntity
    {
        [Key]
        public int ContractID { get; set; }
        public int Id => ContractID; // ánh xạ về Id chung

        [Required]
        public int DealID { get; set; }

        [ForeignKey(nameof(DealID))]
        public Deal? Deal { get; set; }

        [Required]
        [StringLength(5000)]
        public string ContractContent { get; set; } = string.Empty;

        public string? FilePath { get; set; }

        [StringLength(255)]
        public string? FileHash { get; set; }

        [Required]
        [StringLength(50)]
        public string ApprovalStatus { get; set; } = "Pending"; // Pending, Approved, Rejected

        [Required]
        [StringLength(50)]
        public string PaymentStatus { get; set; } = "Pending"; // Pending, Paid, Overdue

        [Required]
        public int CreatedByUserID { get; set; }

        [ForeignKey(nameof(CreatedByUserID))]
        public User? CreatedBy { get; set; } // ✅ trùng với DbContext

        public int? ApprovedByUserID { get; set; }

        [ForeignKey(nameof(ApprovedByUserID))]
        public User? ApprovedBy { get; set; } // ✅ trùng với DbContext

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; } = false;

        public bool IsSensitive { get; set; } = false;
        public DateTime? ApprovedAt { get; set; } // Ngày Manager duyệt hoặc từ chối
    }
}

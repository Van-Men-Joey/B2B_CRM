using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Customer_Relationship_Management.Repositories.Interfaces;

namespace Customer_Relationship_Management.Models
{
    public enum ContractApprovalStatus
    {
        Pending,
        ManagerApproved, // Thêm trạng thái này nếu chưa có trong Enum
        Approved,
        Rejected
    }

    public enum ContractPaymentStatus
    {
        Pending,
        Paid,
        Overdue
    }

    public class Contract : IEntity
    {
        [Key]
        public int ContractID { get; set; }
        public int Id => ContractID;

        [Required]
        public int DealID { get; set; }

        [ForeignKey(nameof(DealID))]
        public Deal? Deal { get; set; }

        [Required]
        public string ContractContent { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? FilePath { get; set; }

        [MaxLength(250)]
        public string? FileHash { get; set; }

        [Required]
        [EnumDataType(typeof(ContractApprovalStatus))]
        [MaxLength(20)]
        public string ApprovalStatus { get; set; } = ContractApprovalStatus.Pending.ToString();

        [Required]
        [EnumDataType(typeof(ContractPaymentStatus))]
        [MaxLength(20)]
        public string PaymentStatus { get; set; } = ContractPaymentStatus.Pending.ToString();

        [Required]
        public int CreatedByUserID { get; set; }

        [ForeignKey(nameof(CreatedByUserID))]
        public User? CreatedBy { get; set; }

        // --- NGƯỜI DUYỆT CẤP 1 (MANAGER) ---
        public int? ApprovedByUserID { get; set; }

        [ForeignKey(nameof(ApprovedByUserID))]
        public User? ApprovedBy { get; set; }

        public DateTime? ApprovedAt { get; set; }

        // --- NGƯỜI DUYỆT CẤP 2 (GIÁM ĐỐC) - BẠN ĐANG THIẾU PHẦN NÀY ---
        public int? DirectorApprovedByUserID { get; set; }

        [ForeignKey(nameof(DirectorApprovedByUserID))]
        public User? DirectorApprovedBy { get; set; }

        public DateTime? DirectorApprovedAt { get; set; }
        // ----------------------------------------------------------------

        [MaxLength(250)]
        public string? QRCodeLink { get; set; }

        public DateTime? PaymentAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; } = false;

        public bool IsSensitive { get; set; } = false;

        // Concurrency token
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Customer_Relationship_Management.Repositories.Interfaces;

namespace Customer_Relationship_Management.Models
{
    public class Deal : IEntity
    {
        [Key]
        public int DealID { get; set; }
        [NotMapped]
        public int Id => DealID; // ánh xạ về Id chung
        [Required(ErrorMessage = "Phải chọn khách hàng.")]
        public int CustomerID { get; set; }

        public int CreatedByUserID { get; set; }

        [Required(ErrorMessage = "Tên deal không được để trống.")]
        [StringLength(200, ErrorMessage = "Tên deal không được vượt quá 200 ký tự.")]
        public string? DealName { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Required(ErrorMessage = "Giá trị deal là bắt buộc.")]
        [Range(1, double.MaxValue, ErrorMessage = "Giá trị deal phải lớn hơn 0.")]
        public decimal Value { get; set; }

        [StringLength(50)]
        public string Stage { get; set; } = "Lead";

        public DateTime? Deadline { get; set; }
        public string? Notes { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("CustomerID")]
        public Customer? Customer { get; set; }

        [ForeignKey("CreatedByUserID")]
        public User? CreatedBy { get; set; }

        public ICollection<Contract>? Contracts { get; set; }
        public ICollection<Task>? Tasks { get; set; }
    }
}

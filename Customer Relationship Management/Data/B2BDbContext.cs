using Microsoft.EntityFrameworkCore;
using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Security;

namespace Customer_Relationship_Management.Data
{
    public class B2BDbContext : DbContext
    {
        public B2BDbContext(DbContextOptions<B2BDbContext> options) : base(options) { }

        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Deal> Deals { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<Models.Task> Tasks { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<SystemRestoreLog> SystemRestoreLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 🔹 Khai báo các bảng có trigger
            modelBuilder.Entity<User>()
                .ToTable(tb => tb.HasTrigger("trg_Users_GenerateCode")); // EF sẽ không dùng OUTPUT khi INSERT

            // 🔹 Khai báo bảng có trigger (để EF bỏ OUTPUT clause khi INSERT/UPDATE)
            modelBuilder.Entity<Customer>()
                .ToTable(tb => tb.HasTrigger("trg_Customers_GenerateCode")); // <-- Đặt đúng tên trigger của bảng Customers
            modelBuilder.Entity<Contract>()
                .ToTable(tb => tb.HasTrigger("trg_Contracts_Audit"));

            // 🔹 Đặt tên bảng cho Task entity
            modelBuilder.Entity<Models.Task>().ToTable("Tasks");

            // 🔹 Task – User (AssignedToUser)
            modelBuilder.Entity<Models.Task>()
                .HasOne(t => t.AssignedToUser)
                .WithMany()
                .HasForeignKey(t => t.AssignedToUserID)
                .OnDelete(DeleteBehavior.Restrict);

            // 🔹 Ràng buộc unique cho mã người dùng và khách hàng
            modelBuilder.Entity<User>().HasIndex(u => u.UserCode).IsUnique();
            modelBuilder.Entity<Customer>().HasIndex(c => c.CustomerCode).IsUnique();

            // 🔹 Contract -> Deal
            modelBuilder.Entity<Contract>()
                .HasOne(c => c.Deal)
                .WithMany(d => d.Contracts)
                .HasForeignKey(c => c.DealID)
                .OnDelete(DeleteBehavior.Restrict);

            // 🔹 Contract -> User (CreatedBy)
            modelBuilder.Entity<Contract>()
                .HasOne(c => c.CreatedBy)
                .WithMany(u => u.ContractsCreated)
                .HasForeignKey(c => c.CreatedByUserID)
                .OnDelete(DeleteBehavior.Restrict);

            // 🔹 Contract -> User (ApprovedBy)
            modelBuilder.Entity<Contract>()
                .HasOne(c => c.ApprovedBy)
                .WithMany(u => u.ContractsApproved)
                .HasForeignKey(c => c.ApprovedByUserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Manager)
                .WithMany(m => m.Employees)
                .HasForeignKey(u => u.ManagerID)
                .OnDelete(DeleteBehavior.Restrict);

            // ✅ Check constraint: Employee (RoleID = 1) must have ManagerID
            modelBuilder.Entity<User>()
                .ToTable(tb =>
                {
                    tb.HasCheckConstraint("CK_Users_ManagerRequiredForEmployee", "[RoleID] <> 1 OR [ManagerID] IS NOT NULL");
                });
            // Khai báo các bảng có trigger để tránh lỗi EF
            modelBuilder.Entity<User>()
                .ToTable("Users", tb => tb.HasTrigger("trg_Users_GenerateCode"));

            modelBuilder.Entity<Customer>()
                .ToTable("Customers", tb => tb.HasTrigger("trg_Customers_GenerateCode"));
        }

        // Seed dữ liệu mẫu (chỉ chạy 1 lần)
        public void EnsureSeedData()
        {
            var now = DateTime.UtcNow;

            // 1. Roles
            if (!Roles.Any())
            {
                Roles.AddRange(
                    new Role {  RoleName = "Employee", Description = "Nhân viên Sale/nhập liệu" },
                    new Role {  RoleName = "Manager", Description = "Quản lý/duyệt deal" },
                    new Role {  RoleName = "Admin", Description = "Quản trị hệ thống" }
                );
                SaveChanges();
            }

            // 2. Users
            if (!Users.Any())
            {
                string hash = PasswordHasher.HashPassword("demo");
                Users.AddRange(
                    new User { UserCode = "EMP001", Username = "employee01", Email = "employee01@example.com", PasswordHash = hash, FullName = "Nhân viên 01", Phone = "0900000001", RoleID = 1, Status = "Active", CreatedAt = now, UpdatedAt = now },
                    new User { UserCode = "MAN001", Username = "manager01", Email = "manager01@example.com", PasswordHash = hash, FullName = "Quản lý 01", Phone = "0900000002", RoleID = 2, Status = "Active", CreatedAt = now, UpdatedAt = now },
                    new User { UserCode = "ADM001", Username = "admin01", Email = "admin01@example.com", PasswordHash = hash, FullName = "Quản trị 01", Phone = "0900000003", RoleID = 3, Status = "Active", CreatedAt = now, UpdatedAt = now }
                );
                SaveChanges();

                // Gán Manager cho Employee mặc định nếu có
                var emp = Users.FirstOrDefault(u => u.Username == "employee01");
                var manager = Users.FirstOrDefault(u => u.Username == "manager01");
                if (emp != null && manager != null)
                {
                    emp.ManagerID = manager.UserID;
                    SaveChanges();
                }
            }


            // 3. Customers
            if (!Customers.Any())
            {
                var customerList = new List<Customer>
    {
        new Customer
        {
           
            CompanyName = "Công ty A",
            Industry = "IT",
            Scale = "Medium",
            Address = "Hà Nội",
            ContactName = "Nguyễn Văn A",
            ContactTitle = "Giám đốc",
            ContactEmail = "contactA@company.com",
            ContactPhone = "0911000001",
            VIP = true,
            AssignedToUserID = 1,
            Notes = "Khách hàng lâu năm",
            CreatedAt = now,
            UpdatedAt = now
        },
        new Customer
        {
            
            CompanyName = "Công ty B",
            Industry = "Logistics",
            Scale = "Large",
            Address = "Hồ Chí Minh",
            ContactName = "Trần Thị B",
            ContactTitle = "Trưởng phòng",
            ContactEmail = "contactB@company.com",
            ContactPhone = "0911000002",
            VIP = false,
            AssignedToUserID = 2,
            Notes = "Khách mới",
            CreatedAt = now,
            UpdatedAt = now
        },
     
    };

                Customers.AddRange(customerList);
                SaveChanges();
            }


            // 4. Deals
            if (!Deals.Any())
            {
                var customerA = Customers.FirstOrDefault(c => c.CompanyName == "Công ty A");
                if (customerA != null)
                {
                    Deals.Add(new Deal
                    {
                        CustomerID = customerA.CustomerID,
                        CreatedByUserID = 1,
                        DealName = "Triển khai CRM cho Công ty A",
                        Stage = "Lead",
                        Value = 50000,
                        Deadline = now.AddMonths(1),
                        Notes = "Bắt đầu tư vấn",
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                    SaveChanges();
                }
                SaveChanges();
            }

            // 5. Contracts
            if (!Contracts.Any())
            {
                Contracts.Add(new Contract { DealID = 2, CreatedByUserID = 1, ContractContent = "Nội dung hợp đồng mẫu", FilePath = "/contracts/contract1.pdf", FileHash = "hash", ApprovalStatus = "Pending", PaymentStatus = "Pending", IsSensitive = false, CreatedAt = now, UpdatedAt = now });
                SaveChanges();
            }

            {
                // Lấy lại ID thực tế từ DB
                var customerA = Customers.FirstOrDefault(c => c.CompanyName == "Công ty A");
                var customerB = Customers.FirstOrDefault(c => c.CompanyName == "Công ty B");
                var dealA = Deals.FirstOrDefault(d => d.DealName.Contains("Công ty A"));
                var user1 = Users.FirstOrDefault(u => u.Username == "employee01");
                var user2 = Users.FirstOrDefault(u => u.Username == "manager01");

                // ✅ 6. Tasks
                if (dealA != null && user1 != null && user2 != null)
                {
                    Tasks.AddRange(
                        new Customer_Relationship_Management.Models.Task
                        {
                            Title = "Gọi điện khách hàng A",
                            Description = "Nhắc nhở lịch trình gặp mặt với Công ty A",
                            AssignedToUserID = user1.UserID,
                            CreatedByUserID = user1.UserID,
                            RelatedDealID = dealA.DealID,
                            DueDate = now.AddDays(3),
                            Status = "Pending",
                            ReminderAt = now.AddDays(2),
                            CreatedAt = now,
                            UpdatedAt = now
                        },
                        new Customer_Relationship_Management.Models.Task
                        {
                            Title = "Chuẩn bị báo giá cho Công ty B",
                            Description = "Soạn báo giá chi tiết cho deal mới",
                            AssignedToUserID = user2.UserID,
                            CreatedByUserID = user2.UserID,
                            RelatedDealID = dealA.DealID, // hoặc có thể null nếu deal chưa có
                            DueDate = now.AddDays(5),
                            Status = "Pending",
                            ReminderAt = now.AddDays(4),
                            CreatedAt = now,
                            UpdatedAt = now
                        }
                    );
                    SaveChanges();
                }

            }

        }
    }
}

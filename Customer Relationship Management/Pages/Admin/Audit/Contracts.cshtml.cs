using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Customer_Relationship_Management.Repositories.Interfaces;
using Customer_Relationship_Management.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Customer_Relationship_Management.Pages.Admin.Audit
{
    [Authorize(Roles = "Admin")]
    public class ContractsModel : PageModel
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IUserRepository _userRepo;

        public List<AuditLog> ManagerLogs { get; set; } = new();
        public List<AuditLog> EmployeeLogs { get; set; } = new();

        public ContractsModel(IAuditLogRepository auditLogRepository, IUserRepository userRepo)
        {
            _auditLogRepository = auditLogRepository;
            _userRepo = userRepo;
        }

        public async Task OnGetAsync()
        {
            // Lấy tất cả log của bảng Contracts (có thể trả về cả log không có User navigation)
            var logs = (await _auditLogRepository.FindAsync(tableName: "Contracts"))
                        .OrderByDescending(l => l.CreatedAt)
                        .ToList();

            var managerList = new List<AuditLog>();
            var employeeList = new List<AuditLog>();

            foreach (var log in logs)
            {
                // Nếu navigation User đã được include thì dùng luôn, nếu không thì cố gắng load từ repository
                User? user = log.User;
                if (user == null && log.UserID.HasValue)
                {
                    try
                    {
                        user = await _userRepo.GetByIdAsync(log.UserID.Value);
                        // gán tạm để view có thể dùng log.User.*
                        log.User = user;
                    }
                    catch
                    {
                        // ignore, user có thể null
                    }
                }

                // Xác định role: ưu tiên RoleID nếu có, fallback Role.RoleName
                var roleIsManager = (user?.RoleID == 2) ||
                                    string.Equals(user?.Role?.RoleName, "Manager", System.StringComparison.OrdinalIgnoreCase);

                var roleIsEmployee = (user?.RoleID == 1) ||
                                     string.Equals(user?.Role?.RoleName, "Employee", System.StringComparison.OrdinalIgnoreCase);

                // Quy tắc phân loại:
                // - Manager logs: người thao tác là Manager
                // - Employee logs: người thao tác là Employee
                // Nếu không biết role, có thể đưa vào employeeList như fallback (tùy ý)
                if (roleIsManager)
                    managerList.Add(log);
                else if (roleIsEmployee)
                    employeeList.Add(log);
                else
                {
                    // Fallback: nếu action là Create -> employee, nếu Update/Delete -> manager? 
                    // Đơn giản đặt Create vào Employee, Update/Delete vào Manager để dễ tra cứu
                    if (log.Action == ActionType.Create)
                        employeeList.Add(log);
                    else
                        managerList.Add(log);
                }
            }

            // Lấy 100 bản ghi mới nhất mỗi bảng
            ManagerLogs = managerList.OrderByDescending(l => l.CreatedAt).Take(100).ToList();
            EmployeeLogs = employeeList.OrderByDescending(l => l.CreatedAt).Take(100).ToList();
        }
    }
}
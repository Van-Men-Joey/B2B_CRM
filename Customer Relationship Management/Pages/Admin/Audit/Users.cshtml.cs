using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace Customer_Relationship_Management.Pages.Admin.Audit
{
    [Authorize(Roles = "Admin")]
    public class UsersModel : PageModel
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IUserRepository _userRepo;

        public List<AuditLog> Logs { get; set; } = new();

        public UsersModel(IAuditLogRepository auditLogRepository, IUserRepository userRepo)
        {
            _auditLogRepository = auditLogRepository;
            _userRepo = userRepo;
        }

        public async Task OnGetAsync()
        {
            // Lấy log của bảng Users, giới hạn 100 bản ghi mới nhất
            var logs = (await _auditLogRepository.FindAsync(tableName: "Users"))
                        .OrderByDescending(l => l.CreatedAt)
                        .ToList();

            // Nếu navigation User (actor) chưa được include, cố gắng load user + role để hiển thị Mã QT và RoleName
            foreach (var log in logs)
            {
                if (log.User == null && log.UserID.HasValue)
                {
                    try
                    {
                        // GetByIdAsync nên trả về user kèm Role nếu repository implement như vậy.
                        var u = await _userRepo.GetByIdAsync(log.UserID.Value);
                        log.User = u;
                    }
                    catch
                    {
                        // ignore nếu không nạp được
                    }
                }
            }

            Logs = logs.Take(100).ToList();
        }
        public Dictionary<string, string> ParseJson(string? json)
        {
            var dict = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(json)) return dict;

            try
            {
                if (json.Contains("\\u0022"))
                    json = JsonSerializer.Deserialize<string>(json);

                using var doc = JsonDocument.Parse(json);
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (new[] { "UserID", "UserCode", "Username", "FullName", "Email", "RoleID", "RoleName", "Status", "IsDeleted" }
                        .Contains(prop.Name))
                    {
                        dict[prop.Name] = prop.Value.ToString();
                    }
                }
            }
            catch
            {
                dict["RawData"] = json ?? "";
            }

            return dict;
        }
    }
}
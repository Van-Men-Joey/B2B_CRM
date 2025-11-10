using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Customer_Relationship_Management.Repositories.Interfaces;
using Customer_Relationship_Management.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;

namespace Customer_Relationship_Management.Pages.Admin.Audit
{
    [Authorize(Roles = "Admin")]
    public class CustomersModel : PageModel
    {
        private readonly IAuditLogRepository _auditLogRepository;

        public List<AuditLog> Logs { get; set; } = new();

        public CustomersModel(IAuditLogRepository auditLogRepository)
        {
            _auditLogRepository = auditLogRepository;
        }

        public async Task OnGetAsync()
        {
            // 🔍 Lấy log của bảng Customers, giới hạn 100 bản ghi mới nhất
            var logs = await _auditLogRepository.FindAsync(tableName: "Customers");

            Logs = logs
                .OrderByDescending(l => l.CreatedAt)
                .Take(100)
                .ToList();
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
                    dict[prop.Name] = prop.Value.ToString();
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

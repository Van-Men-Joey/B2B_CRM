using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace Customer_Relationship_Management.Pages.Admin.Audit
{
    public class DealsModel : PageModel
    {
        private readonly IAuditLogRepository _auditLogRepository;

        public List<AuditLog> Logs { get; set; } = new();
        public List<AuditLog> AllLogs { get; set; } = new(); // giữ toàn bộ log

        public DealsModel(IAuditLogRepository auditLogRepository)
        {
            _auditLogRepository = auditLogRepository;
        }

        public string Role { get; set; } = "Employee"; // default

        // Filter properties
        [BindProperty]
        public DateTime? FilterFromDate { get; set; }

        [BindProperty]
        public DateTime? FilterToDate { get; set; }

        [BindProperty]
        public string? FilterRole { get; set; }

        [BindProperty]
        public string? FilterAction { get; set; }

        public async Task OnGetAsync()
        {
            Role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? "Employee";

            var logs = await _auditLogRepository.FindAsync(tableName: "Deals");

            AllLogs = logs.OrderByDescending(l => l.CreatedAt).Take(100).ToList();
            Logs = AllLogs.ToList();
        }

        public void ApplyFilters()
        {
            IEnumerable<AuditLog> filtered = AllLogs;

            if (FilterFromDate.HasValue)
                filtered = filtered.Where(l => l.CreatedAt.Date >= FilterFromDate.Value.Date);

            if (FilterToDate.HasValue)
                filtered = filtered.Where(l => l.CreatedAt.Date <= FilterToDate.Value.Date);

            if (!string.IsNullOrEmpty(FilterRole))
                filtered = filtered.Where(l => l.User != null && l.User.Role.ToString() == FilterRole);

            if (!string.IsNullOrEmpty(FilterAction))
                filtered = filtered.Where(l => l.Action.ToString() == FilterAction);

            Logs = filtered.ToList();
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

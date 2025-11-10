using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Customer_Relationship_Management.Pages.Manager.Customer
{
    public class IndexModel : PageModel
    {
        private readonly ICustomerService _customerService;
        private readonly IUserService _userService;

        public IndexModel(ICustomerService customerService, IUserService userService)
        {
            _customerService = customerService;
            _userService = userService;
        }

        // Dashboard
        public int TotalCustomers { get; set; }
        public int TotalVIP { get; set; }
        public int TotalDeals { get; set; }
        public decimal TotalDealValue { get; set; }

        // Table data
        public List<CustomerRowViewModel> Customers { get; set; } = new();

        [BindProperty(SupportsGet = true)] public string SearchTerm { get; set; } = "";
        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
        [BindProperty(SupportsGet = true)] public string SortBy { get; set; } = "CompanyName";
        [BindProperty(SupportsGet = true)] public string SortDir { get; set; } = "asc";

        public int PageSize { get; set; } = 12;
        public int TotalPages { get; set; }

        // Assign user
        public List<User> Employees { get; set; } = new();
        [BindProperty] public int? SelectedEmployeeId { get; set; }
        [BindProperty] public int AssignCustomerId { get; set; }

        // VIP toggle
        [BindProperty] public int VipCustomerId { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAssignEmployeeAsync()
        {
            if (SelectedEmployeeId == null)
            {
                TempData["Error"] = "Vui lòng chọn nhân viên.";
                await LoadDataAsync();
                return Page();
            }

            var customer = await _customerService.GetCustomerByCustomerID(AssignCustomerId);
            if (customer == null)
            {
                TempData["Error"] = "Không tìm thấy khách hàng.";
                return RedirectToPage();
            }

            var managerId = GetCurrentUserId();
            var (success, message) = await _customerService.ReassignCustomerAsync(
                AssignCustomerId, SelectedEmployeeId.Value, managerId
            );

            TempData[success ? "Success" : "Error"] = message;
            return RedirectToPage(new { SearchTerm, PageNumber, SortBy, SortDir });
        }

        public async Task<IActionResult> OnPostToggleVIPAsync()
        {
            var managerId = GetCurrentUserId();
            var (success, message) = await _customerService.ToggleVIPAsync(VipCustomerId, managerId);

            TempData[success ? "Success" : "Error"] = message;
            return RedirectToPage(new { SearchTerm, PageNumber, SortBy, SortDir });
        }

        private async Task LoadDataAsync()
        {
            var managerId = GetCurrentUserId();

            // Lấy tất cả user thuộc đội của Manager
            var allUsers = await _userService.GetAllAsync();
            Employees = allUsers.Where(u => !u.IsDeleted && u.RoleID != 3 && u.ManagerID == managerId).ToList();

            // Lấy danh sách userId team
            var teamUserIds = Employees.Select(e => e.UserID).Append(managerId).ToList();

            // Lấy khách của team
            var raw = await _customerService.GetCustomersForTeamAsync(teamUserIds);
            var list = raw.Where(c => !c.IsDeleted).ToList();

            // Search
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var q = SearchTerm.ToLower();
                list = list.Where(c =>
                    (c.CompanyName?.ToLower().Contains(q) ?? false) ||
                    (c.ContactName?.ToLower().Contains(q) ?? false) ||
                    (c.ContactEmail?.ToLower().Contains(q) ?? false) ||
                    (c.ContactPhone?.ToLower().Contains(q) ?? false)
                ).ToList();
            }

            // Map to VM
            var rows = list.Select(c => new CustomerRowViewModel
            {
                CustomerID = c.CustomerID,
                CompanyName = c.CompanyName,
                Industry = c.Industry,
                ContactName = c.ContactName,
                ContactEmail = c.ContactEmail,
                ContactPhone = c.ContactPhone,
                AssignedToUserID = c.AssignedToUserID,
                VIP = c.VIP,
                DealsCount = c.Deals?.Count ?? 0,
                DealsValue = c.Deals?.Sum(d => d.Value) ?? 0
            }).ToList();

            // Sort
            rows = (SortBy.ToLower(), SortDir.ToLower()) switch
            {
                ("deals", "asc") => rows.OrderBy(r => r.DealsCount).ToList(),
                ("deals", "desc") => rows.OrderByDescending(r => r.DealsCount).ToList(),
                ("dealvalue", "asc") => rows.OrderBy(r => r.DealsValue).ToList(),
                ("dealvalue", "desc") => rows.OrderByDescending(r => r.DealsValue).ToList(),
                ("companyname", "desc") => rows.OrderByDescending(r => r.CompanyName).ToList(),
                _ => rows.OrderBy(r => r.CompanyName).ToList()
            };

            // Paging
            TotalCustomers = rows.Count;
            TotalPages = (int)Math.Ceiling(TotalCustomers / (double)PageSize);
            PageNumber = Math.Clamp(PageNumber, 1, Math.Max(1, TotalPages));
            Customers = rows.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();

            // Dashboard
            TotalVIP = rows.Count(r => r.VIP);
            TotalDeals = rows.Sum(r => r.DealsCount);
            TotalDealValue = rows.Sum(r => r.DealsValue);
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirst("UserID"); // đọc đúng key

            return idClaim != null && int.TryParse(idClaim.Value, out var id)
                ? id
                : 0;
        }


        public class CustomerRowViewModel
        {
            public int CustomerID { get; set; }
            public string? CompanyName { get; set; }
            public string? Industry { get; set; }
            public string? ContactName { get; set; }
            public string? ContactEmail { get; set; }
            public string? ContactPhone { get; set; }
            public int? AssignedToUserID { get; set; }
            public bool VIP { get; set; }
            public int DealsCount { get; set; }
            public decimal DealsValue { get; set; }
        }
    }
}

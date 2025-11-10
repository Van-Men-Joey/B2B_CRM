using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Repositories.Interfaces;
using Customer_Relationship_Management.Services.Interfaces;
using System.Text.RegularExpressions;

namespace Customer_Relationship_Management.Services.Implements
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IAuditLogService _auditLogService;

        public CustomerService(ICustomerRepository customerRepository, IAuditLogService auditLogService)
        {
            _customerRepository = customerRepository;
            _auditLogService = auditLogService;
        }

        public async Task<IEnumerable<Customer>> GetCustomersForUserAsync(int userId)
        {
            return await _customerRepository.GetByAssignedUserAsync(userId);
        }

        public async Task<Customer?> GetCustomerByCustomerID_UserIDAsync(int customerId, int userId)
        {
            var c = await _customerRepository.GetByIdAsync(customerId);
            if (c != null && c.AssignedToUserID == userId && !c.IsDeleted)
                return c;
            return null;
        }
        public async Task<Customer?> GetCustomerByCustomerID(int customerId)
        {
            var c = await _customerRepository.GetByIdAsync(customerId);
            if (c != null && !c.IsDeleted)
                return c;
            return null;
        }

        public async Task<(bool Success, string Message)> AddCustomerAsync(Customer customer, int userId)
        {
            // ✅ Kiểm tra định dạng email
            if (!string.IsNullOrWhiteSpace(customer.ContactEmail) &&
                !Regex.IsMatch(customer.ContactEmail, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                return (false, "Email không hợp lệ.");
            }

            // ✅ Kiểm tra trùng email
            if (!string.IsNullOrWhiteSpace(customer.ContactEmail))
            {
                var existsEmail = await _customerRepository.FindAsync(c =>
                    c.ContactEmail.ToLower() == customer.ContactEmail.ToLower() && !c.IsDeleted);
                if (existsEmail.Any())
                    return (false, "Email này đã tồn tại.");
            }

            // ✅ Kiểm tra trùng số điện thoại
            if (!string.IsNullOrWhiteSpace(customer.ContactPhone))
            {
                var existsPhone = await _customerRepository.FindAsync(c =>
                    c.ContactPhone.Replace(" ", "") == customer.ContactPhone.Replace(" ", "") && !c.IsDeleted);
                if (existsPhone.Any())
                    return (false, "Số điện thoại này đã tồn tại.");
            }

            // ✅ Thêm mới
            customer.AssignedToUserID = userId;
            customer.CreatedAt = DateTime.UtcNow;
            customer.UpdatedAt = DateTime.UtcNow;
            customer.IsDeleted = false;

            try
            {
                await _customerRepository.AddAsync(customer);
                await _customerRepository.SaveChangesAsync();

                // 🧾 Ghi log tạo mới
                await _auditLogService.LogAsync(
                    userId: userId,
                    action: ActionType.Create,
                    tableName: "Customers",
                    recordId: customer.CustomerID.ToString(),
                    oldValue: null,
                    newValue: customer
                );

                return (true, "Thêm khách hàng thành công.");
            }
            catch (Exception ex)
            {
                await _auditLogService.LogAsync(
                    userId: userId,
                    action: ActionType.Update,
                    tableName: "Customers",
                    recordId: "N/A",
                    oldValue: null,
                    newValue: new { Error = ex.Message }
                );

                return (false, "Lỗi khi lưu dữ liệu.");
            }
        }

        public async Task<bool> UpdateCustomerAsync(Customer customer, int userId)
        {
            // ✅ Bước 1: Lấy dữ liệu cũ (NoTracking) để log
            var oldEntity = await _customerRepository.GetByIdAsNoTrackingAsync(customer.CustomerID);
            if (oldEntity == null || oldEntity.AssignedToUserID != userId)
                return false;

            var oldData = new
            {
                oldEntity.CustomerCode,
                oldEntity.CompanyName,
                oldEntity.Industry,
                oldEntity.Scale,
                oldEntity.Address,
                oldEntity.ContactName,
                oldEntity.ContactTitle,
                oldEntity.ContactEmail,
                oldEntity.ContactPhone,
                oldEntity.Notes
            };
            var oldValueJson = AuditLog.ToJson(oldData);

            // ✅ Bước 2: Lấy lại entity có tracking để cập nhật
            var existing = await _customerRepository.GetByIdAsync(customer.CustomerID);
            if (existing == null) return false;
            existing.CustomerCode = customer.CustomerCode;
            existing.CompanyName = customer.CompanyName;
            existing.Industry = customer.Industry;
            existing.Scale = customer.Scale;
            existing.Address = customer.Address;
            existing.ContactName = customer.ContactName;
            existing.ContactTitle = customer.ContactTitle;
            existing.ContactEmail = customer.ContactEmail;
            existing.ContactPhone = customer.ContactPhone;
            existing.Notes = customer.Notes;
            existing.UpdatedAt = DateTime.UtcNow;

            await _customerRepository.UpdateAsync(existing);
            await _customerRepository.SaveChangesAsync();

            var newData = new
            {
                customer.CustomerCode,
                customer.CompanyName,
                customer.Industry,
                customer.Scale,
                customer.Address,
                customer.ContactName,
                customer.ContactTitle,
                customer.ContactEmail,
                customer.ContactPhone,
                customer.Notes
            };

            await _auditLogService.LogAsync(
                userId: userId,
                action: ActionType.Update,
                tableName: "Customers",
                recordId: existing.CustomerID.ToString(),
                oldValue: oldValueJson,
                newValue: newData
            );

            return true;
        }

        public async Task<bool> DeleteCustomerAsync(int customerId, int userId)
        {
            var existing = await _customerRepository.GetByIdAsync(customerId);
            if (existing == null || existing.AssignedToUserID != userId)
                return false;

            existing.IsDeleted = true;
            existing.UpdatedAt = DateTime.UtcNow;

            await _customerRepository.UpdateAsync(existing);
            await _customerRepository.SaveChangesAsync();

            await _auditLogService.LogAsync(
                userId: userId,
                action: ActionType.Delete,
                tableName: "Customers",
                recordId: existing.CustomerID.ToString(),
                oldValue: existing,
                newValue: new { IsDeleted = true }
            );

            return true;
        }


        //Manager
        public async Task<IEnumerable<Customer>> GetCustomersForTeamAsync(IEnumerable<int> teamUserIds)
        {
            return await _customerRepository.GetByTeamUsersAsync(teamUserIds);
        }

        public async Task<(bool Success, string Message)> ReassignCustomerAsync(int customerId, int newUserId, int managerId)
        {
            var customer = await _customerRepository.GetByIdAsync(customerId);
            if (customer == null || customer.IsDeleted)
                return (false, "Khách hàng không tồn tại.");

            var oldUserId = customer.AssignedToUserID;

            var result = await _customerRepository.ReassignCustomerAsync(customerId, newUserId);
            if (!result) return (false, "Không thể chuyển khách.");

            await _auditLogService.LogAsync(
                userId: managerId,
                action: ActionType.Update,
                tableName: "Customers",
                recordId: customerId.ToString(),
                oldValue: new { AssignedTo = oldUserId },
                newValue: new { AssignedTo = newUserId }
            );

            return (true, "Chuyển khách thành công.");
        }

        public async Task<(bool Success, string Message)> ToggleVIPAsync(int customerId, int managerId)
        {
            var customer = await _customerRepository.GetByIdAsync(customerId);
            if (customer == null) return (false, "Khách hàng không tồn tại.");

            bool newVipStatus = !customer.VIP;
            await _customerRepository.UpdateVIPStatusAsync(customerId, newVipStatus);

            await _auditLogService.LogAsync(
                userId: managerId,
                action: ActionType.Update,
                tableName: "Customers",
                recordId: customerId.ToString(),
                oldValue: new { VIP = !newVipStatus },
                newValue: new { VIP = newVipStatus }
            );

            return (true, newVipStatus ? "Đã đánh dấu VIP." : "Đã bỏ VIP.");
        }
    }
}

using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Repositories.Interfaces;
using Customer_Relationship_Management.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Customer_Relationship_Management.Services.Implements
{
    /// <summary>
    /// Triển khai nghiệp vụ cho Deal (Cơ hội kinh doanh)
    /// </summary>
    public class DealService : IDealService
    {
        private readonly IDealRepository _dealRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IUserRepository _userRepository;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<DealService> _logger;

        public DealService(
            IDealRepository dealRepository,
            ICustomerRepository customerRepository,
            IUserRepository userRepository,
            IAuditLogService auditLogService,
            ILogger<DealService> logger)
        {
            _dealRepository = dealRepository;
            _customerRepository = customerRepository;
            _userRepository = userRepository;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        // ===========================================================
        // 🧍‍♂️ KHU VỰC NHÂN VIÊN
        // ===========================================================

        public async Task<IEnumerable<Deal>> GetDealsByEmployeeAsync(int employeeId)
            => await _dealRepository.GetDealsByEmployeeAsync(employeeId);

        public async Task<Deal?> GetDealByIdAsync(int dealId, int employeeId)
        {
            var deal = await _dealRepository.GetDealWithCustomerByIdAsync(dealId);
            if (deal == null || deal.IsDeleted)
                return null;

            if (deal.Customer == null || deal.Customer.AssignedToUserID != employeeId)
                return null;

            return deal;
        }

        public async Task<(bool Success, string Message)> CreateDealAsync(Deal deal)
        {
            // ✅ Kiểm tra dữ liệu đầu vào
            if (string.IsNullOrWhiteSpace(deal.DealName))
                return (false, "Tên deal không được để trống.");
            if (deal.Value <= 0)
                return (false, "Giá trị deal phải lớn hơn 0.");

            var customer = await _customerRepository.GetByIdAsync(deal.CustomerID);
            if (customer == null || customer.IsDeleted)
                return (false, "Khách hàng không tồn tại hoặc đã bị xóa.");

            // ✅ Chuẩn hóa dữ liệu
            deal.Stage ??= "Lead";
            deal.IsDeleted = false;
            deal.CreatedAt = DateTime.UtcNow;
            deal.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _dealRepository.AddAsync(deal);
                await _dealRepository.SaveChangesAsync();

                // 🧾 Ghi log tạo mới
                await _auditLogService.LogAsync(
                    userId: deal.CreatedByUserID,
                    action: ActionType.Create,
                    tableName: "Deals",
                    recordId: deal.DealID.ToString(),
                    oldValue: null,
                    newValue: new
                    {
                        deal.DealName,
                        deal.Value,
                        deal.Stage,
                        deal.Deadline,
                        deal.CustomerID,
                        deal.Notes,
                        deal.CreatedAt
                    }
                );

                return (true, "Thêm deal thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi tạo deal: {Message}", ex.Message);

                await _auditLogService.LogAsync(
                    deal.CreatedByUserID,
                    ActionType.Error,
                    "Deals",
                    "N/A",
                    null,
                    new { Error = ex.Message }
                );

                return (false, "Lỗi khi lưu dữ liệu deal.");
            }
        }

        public async Task<(bool Success, string Message)> UpdateDealAsync(Deal deal, int employeeId)
        {
            // ✅ Bước 1: Lấy dữ liệu cũ (NoTracking) để log
            var oldEntity = await _dealRepository.GetByIdAsNoTrackingAsync(
      deal.DealID,
      d => d.Customer
  );

            if (oldEntity == null || oldEntity.IsDeleted)
                return (false, "Deal không tồn tại.");
            if (oldEntity.Customer == null || oldEntity.Customer.AssignedToUserID != employeeId)
                return (false, "Bạn không có quyền chỉnh sửa deal này.");

            var oldData = new
            {
                oldEntity.DealName,
                oldEntity.Value,
                oldEntity.Stage,
                oldEntity.Deadline,
                oldEntity.Notes
            };
            var oldValueJson = AuditLog.ToJson(oldData);

            try
            {
                // ✅ Bước 2: Cập nhật dữ liệu
                var existing = await _dealRepository.GetByIdAsync(deal.DealID );
                if (existing == null)
                    return (false, "Không tìm thấy deal cần cập nhật.");

                existing.DealName = deal.DealName;
                existing.Value = deal.Value;
                existing.Stage = deal.Stage;
                existing.Deadline = deal.Deadline;
                existing.Notes = deal.Notes;
                existing.UpdatedAt = DateTime.UtcNow;

                await _dealRepository.UpdateAsync(existing);
                await _dealRepository.SaveChangesAsync();

                var newData = new
                {
                    existing.DealName,
                    existing.Value,
                    existing.Stage,
                    existing.Deadline,
                    existing.Notes
                };

                await _auditLogService.LogAsync(
                    employeeId,
                    ActionType.Update,
                    "Deals",
                    existing.DealID.ToString(),
                    oldValueJson,
                    newData
                );

                return (true, "Cập nhật deal thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi cập nhật deal {DealID}: {Message}", deal.DealID, ex.Message);
                return (false, "Lỗi khi cập nhật deal.");
            }
        }

        public async Task<(bool Success, string Message)> SoftDeleteAsync(int dealId, int employeeId)
        {
            var existing = await _dealRepository.GetDealWithCustomerByIdAsync(dealId);
            if (existing == null || existing.IsDeleted)
                return (false, "Deal không tồn tại.");

            if (existing.Customer == null || existing.Customer.AssignedToUserID != employeeId)
                return (false, "Bạn không có quyền xóa deal này.");

            try
            {
                existing.IsDeleted = true;
                existing.UpdatedAt = DateTime.UtcNow;

                await _dealRepository.UpdateAsync(existing);
                await _dealRepository.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    employeeId,
                    ActionType.Delete,
                    "Deals",
                    dealId.ToString(),
                    new
                    {
                        existing.DealName,
                        existing.Value,
                        existing.Stage,
                        existing.Deadline
                    },
                    new { IsDeleted = true }
                );

                return (true, "Xóa deal thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi xóa mềm deal {DealID}: {Message}", dealId, ex.Message);
                return (false, "Lỗi khi xóa deal.");
            }
        }

        public async Task<bool> UpdateDealStageAsync(int dealId, string newStage, int employeeId)
        {
            var deal = await _dealRepository.GetDealWithCustomerByIdAsync(dealId);
            if (deal == null || deal.IsDeleted)
                return false;

            if (deal.Customer == null || deal.Customer.AssignedToUserID != employeeId)
                return false;

            var oldStage = deal.Stage;

            try
            {
                deal.Stage = newStage;
                deal.UpdatedAt = DateTime.UtcNow;

                await _dealRepository.UpdateAsync(deal);
                await _dealRepository.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    employeeId,
                    ActionType.Update,
                    "Deals",
                    dealId.ToString(),
                    new { Stage = oldStage },
                    new { Stage = newStage }
                );

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi cập nhật stage deal {DealID}: {Message}", dealId, ex.Message);
                return false;
            }
        }

        // ===========================================================
        // 👔 KHU VỰC MANAGER
        // ===========================================================

        public async Task<IEnumerable<Deal>> GetTeamDealsAsync(int managerId)
            => await _dealRepository.GetTeamDealsAsync(managerId);

        public async Task<IEnumerable<Deal>> FilterTeamDealsAsync(
            int managerId,
            string? stage = null,
            decimal? minValue = null,
            decimal? maxValue = null,
            DateTime? deadlineBefore = null)
            => await _dealRepository.FilterTeamDealsAsync(managerId, stage, minValue, maxValue, deadlineBefore);

        public async Task<IDictionary<string, decimal>> GetTeamPipelineSummaryAsync(int managerId)
            => await _dealRepository.GetTeamPipelineSummaryAsync(managerId);

        public async Task<(bool Success, string Message)> ReassignDealAsync(int dealId, int newEmployeeId, int managerId)
        {
            var deal = await _dealRepository.GetByIdAsync(dealId);
            if (deal == null || deal.IsDeleted)
                return (false, "Deal không tồn tại.");

            var oldEmployeeId = deal.CreatedByUserID;

            try
            {
                var result = await _dealRepository.ReassignDealAsync(dealId, newEmployeeId, managerId);
                if (!result)
                    return (false, "Không thể chuyển giao deal.");

                await _auditLogService.LogAsync(
                    managerId,
                    ActionType.Update,
                    "Deals",
                    dealId.ToString(),
                    new { AssignedTo = oldEmployeeId },
                    new { AssignedTo = newEmployeeId }
                );

                return (true, "Chuyển giao deal thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi chuyển giao deal {DealID}: {Message}", dealId, ex.Message);
                return (false, "Lỗi khi chuyển giao deal.");
            }
        }

        // ===========================================================
        // 🔍 TIỆN ÍCH KHÁC
        // ===========================================================

        public async Task<IEnumerable<Deal>> GetDealsByStageAsync(string stage, int? employeeId = null)
            => await _dealRepository.GetDealsByStageAsync(stage, employeeId);

        public async Task<IEnumerable<Deal>> SearchDealsAsync(int employeeId, string keyword)
            => string.IsNullOrWhiteSpace(keyword)
                ? Enumerable.Empty<Deal>()
                : await _dealRepository.SearchDealsAsync(employeeId, keyword);

        public async Task<IEnumerable<Deal>> GetDealsNearDeadlineAsync(int employeeId, int daysAhead = 7)
            => await _dealRepository.GetDealsNearDeadlineAsync(employeeId, daysAhead);

        public async Task<decimal> GetTotalDealValueByStageAsync(int employeeId, string stage)
            => await _dealRepository.GetTotalDealValueByStageAsync(employeeId, stage);

        public async Task<IEnumerable<string>> GetDealFilesAsync(int dealId)
            => await _dealRepository.GetDealFilesAsync(dealId);
    }
}

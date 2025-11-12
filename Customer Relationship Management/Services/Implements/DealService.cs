using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Repositories.Interfaces;
using Customer_Relationship_Management.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Customer_Relationship_Management.Services.Implements
{
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

        public async Task<IEnumerable<Deal>> GetDealsByEmployeeAsync(int employeeId)
            => await _dealRepository.GetDealsByEmployeeAsync(employeeId);

        public async Task<Deal?> GetDealByIdAsync(int dealId, int employeeId)
        {
            var deal = await _dealRepository.GetDealWithCustomerByIdAsync(dealId);
            if (deal == null || deal.IsDeleted) return null;

            if (deal.Customer?.AssignedToUserID != employeeId) return null;

            return deal;
        }

        public async Task<(bool Success, string Message)> CreateDealAsync(Deal deal)
        {
            if (string.IsNullOrWhiteSpace(deal.DealName))
                return (false, "Tên deal không được để trống.");
            if (deal.Value <= 0)
                return (false, "Giá trị deal phải lớn hơn 0.");

            var customer = await _customerRepository.GetByIdAsync(deal.CustomerID);
            if (customer == null)
                return (false, "Khách hàng không tồn tại.");

            // Kiểm tra quyền sở hữu khách hàng khi tạo deal
            // - Nếu Customer đã có AssignedToUserID: phải trùng với người tạo
            // - Nếu Customer chưa được assign: cho phép tạo
            if (customer.AssignedToUserID.HasValue &&
                customer.AssignedToUserID.Value != deal.CreatedByUserID)
            {
                return (false, "Bạn không được phép tạo deal cho khách hàng này.");
            }

            deal.CreatedAt = DateTime.UtcNow;
            deal.UpdatedAt = DateTime.UtcNow;
            deal.Stage ??= "Lead";
            deal.IsDeleted = false;

            try
            {
                await _dealRepository.AddAsync(deal);
                await _dealRepository.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    deal.CreatedByUserID,
                    ActionType.Create,
                    "Deals",
                    deal.DealID.ToString(),
                    null,
                    deal
                );

                return (true, "Tạo deal thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi tạo deal: {Message}", ex.Message);
                return (false, "Lỗi khi lưu dữ liệu deal.");
            }
        }

        public async Task<(bool Success, string Message)> UpdateDealAsync(Deal deal, int employeeId)
        {
            var existing = await _dealRepository.GetDealWithCustomerByIdAsync(deal.DealID);
            if (existing == null || existing.IsDeleted)
                return (false, "Deal không tồn tại.");

            if (existing.Customer == null || existing.Customer.AssignedToUserID != employeeId)
                return (false, "Bạn không có quyền chỉnh sửa deal này.");

            // Nếu đổi khách hàng, phải kiểm tra quyền sở hữu khách hàng mới
            if (deal.CustomerID != existing.CustomerID)
            {
                var newCustomer = await _customerRepository.GetByIdAsync(deal.CustomerID);
                if (newCustomer == null)
                    return (false, "Khách hàng không tồn tại.");
                if (newCustomer.AssignedToUserID != employeeId)
                    return (false, "Bạn không phụ trách khách hàng này.");
                existing.CustomerID = deal.CustomerID;
            }

            var oldData = AuditLog.ToJson(existing);

            try
            {
                existing.DealName = deal.DealName;
                existing.Value = deal.Value;
                existing.Stage = string.IsNullOrWhiteSpace(deal.Stage) ? existing.Stage : deal.Stage!;
                existing.Deadline = deal.Deadline;
                existing.Notes = deal.Notes;
                existing.UpdatedAt = DateTime.UtcNow;

                _dealRepository.Update(existing);
                await _dealRepository.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    employeeId,
                    ActionType.Update,
                    "Deals",
                    existing.DealID.ToString(),
                    oldData,
                    existing
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
            try
            {
                await _dealRepository.SoftDeleteAsync(dealId, employeeId);

                await _auditLogService.LogAsync(
                    employeeId,
                    ActionType.Delete,
                    "Deals",
                    dealId.ToString(),
                    null,
                    new { IsDeleted = true }
                );

                return (true, "Xóa deal thành công.");
            }
            catch (UnauthorizedAccessException ex)
            {
                return (false, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi xóa mềm deal {DealID}: {Message}", dealId, ex.Message);
                return (false, "Lỗi khi xóa deal.");
            }
        }

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

        public async Task<bool> UpdateDealStageAsync(int dealId, string newStage, int employeeId)
        {
            try
            {
                var result = await _dealRepository.UpdateDealStageAsync(dealId, newStage);
                if (result)
                {
                    await _auditLogService.LogAsync(
                        employeeId,
                        ActionType.Update,
                        "Deals",
                        dealId.ToString(),
                        new { Stage = "Old" },
                        new { Stage = newStage }
                    );
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi cập nhật stage deal: {Message}", ex.Message);
                return false;
            }
        }

        public async Task<IEnumerable<Deal>> GetTeamDealsAsync(int managerId)
            => await _dealRepository.GetTeamDealsAsync(managerId);

        public async Task<IEnumerable<Deal>> FilterTeamDealsAsync(
            int managerId, string? stage = null, decimal? minValue = null, decimal? maxValue = null, DateTime? deadlineBefore = null)
            => await _dealRepository.FilterTeamDealsAsync(managerId, stage, minValue, maxValue, deadlineBefore);

        public async Task<IDictionary<string, decimal>> GetTeamPipelineSummaryAsync(int managerId)
            => await _dealRepository.GetTeamPipelineSummaryAsync(managerId);

        public async Task<(bool Success, string Message)> ReassignDealAsync(int dealId, int newEmployeeId, int managerId)
        {
            try
            {
                var result = await _dealRepository.ReassignDealAsync(dealId, newEmployeeId, managerId);
                if (result)
                {
                    await _auditLogService.LogAsync(
                        managerId,
                        ActionType.Update,
                        "Deals",
                        dealId.ToString(),
                        new { Old = "Previous employee" },
                        new { NewEmployeeId = newEmployeeId }
                    );
                    return (true, "Chuyển giao deal thành công.");
                }
                return (false, "Không thể chuyển giao deal.");
            }
            catch (UnauthorizedAccessException ex)
            {
                return (false, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi chuyển giao deal {DealID}: {Message}", dealId, ex.Message);
                return (false, "Lỗi khi chuyển giao deal.");
            }
        }

        public async Task<IEnumerable<string>> GetDealFilesAsync(int dealId)
            => await _dealRepository.GetDealFilesAsync(dealId);
    }
}
using Customer_Relationship_Management.Models;

namespace Customer_Relationship_Management.Repositories.Interfaces
{
    public interface IDealRepository : IGenericRepository<Deal>
    {
        // ---------------------- NHÂN VIÊN ----------------------
        /// <summary>
        /// Lấy tất cả deal của một nhân viên (chưa bị xóa)
        /// </summary>
        Task<IEnumerable<Deal>> GetDealsByEmployeeAsync(int employeeId);

        /// <summary>
        /// Lọc deal theo giai đoạn (Stage)
        /// </summary>
        Task<IEnumerable<Deal>> GetDealsByStageAsync(string stage, int? employeeId = null);

        /// <summary>
        /// Lấy deal theo ID kèm thông tin khách hàng
        /// </summary>
        Task<Deal?> GetDealWithCustomerByIdAsync(int dealId);

        /// <summary>
        /// Xóa mềm — cập nhật IsDeleted = true thay vì xóa khỏi DB
        /// </summary>
        Task SoftDeleteAsync(int dealId, int currentUserId);

        /// <summary>
        /// Tìm kiếm deal theo tên hoặc khách hàng
        /// </summary>
        Task<IEnumerable<Models.Deal>> SearchDealsAsync(int employeeId, string keyword);

        /// <summary>
        /// Lọc deal sắp đến hạn (deadline trong vòng X ngày)
        /// </summary>
        Task<IEnumerable<Deal>> GetDealsNearDeadlineAsync(int employeeId, int daysAhead = 7);

        /// <summary>
        /// Tính tổng giá trị deal theo giai đoạn (dùng cho dashboard)
        /// </summary>
        Task<decimal> GetTotalDealValueByStageAsync(int employeeId, string stage);

        /// <summary>
        /// Cập nhật giai đoạn (Stage) của Deal
        /// </summary>
        Task<bool> UpdateDealStageAsync(int dealId, string newStage);


        // ---------------------- MANAGER ----------------------
        /// <summary>
        /// Lấy tất cả deal của team mà manager đang quản lý
        /// (bao gồm các nhân viên có ManagerID = managerId)
        /// </summary>
        Task<IEnumerable<Deal>> GetTeamDealsAsync(int managerId);

        /// <summary>
        /// Lọc deal của team theo stage, giá trị hoặc deadline
        /// </summary>
        Task<IEnumerable<Deal>> FilterTeamDealsAsync(
            int managerId,
            string? stage = null,
            decimal? minValue = null,
            decimal? maxValue = null,
            DateTime? deadlineBefore = null
        );

        /// <summary>
        /// Thống kê pipeline toàn team theo stage
        /// </summary>
        Task<IDictionary<string, decimal>> GetTeamPipelineSummaryAsync(int managerId);

        /// <summary>
        /// Chuyển giao deal cho nhân viên khác trong team
        /// (thay đổi CreatedByUserID)
        /// </summary>
        Task<bool> ReassignDealAsync(int dealId, int newEmployeeId, int managerId);

        /// <summary>
        /// Lấy danh sách file hợp đồng/tài liệu thuộc deal (Contracts.FilePath)
        /// </summary>
        Task<IEnumerable<string>> GetDealFilesAsync(int dealId);
    }
}

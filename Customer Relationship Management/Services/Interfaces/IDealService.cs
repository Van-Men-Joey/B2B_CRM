using Customer_Relationship_Management.Models;

namespace Customer_Relationship_Management.Services.Interfaces
{
    /// <summary>
    /// Định nghĩa các nghiệp vụ xử lý liên quan đến Deal (Cơ hội kinh doanh)
    /// </summary>
    public interface IDealService
    {
        // =====================================================================
        // 🧍‍♂️ KHU VỰC NHÂN VIÊN
        // =====================================================================

        /// <summary>
        /// Lấy tất cả deal của nhân viên
        /// </summary>
        Task<IEnumerable<Deal>> GetDealsByEmployeeAsync(int employeeId);

        /// <summary>
        /// Lấy deal theo ID (bao gồm thông tin khách hàng)
        /// </summary>
        Task<Deal?> GetDealByIdAsync(int dealId, int employeeId);

        /// <summary>
        /// Tạo mới deal
        /// </summary>
        Task<(bool Success, string Message)> CreateDealAsync(Deal deal);

        /// <summary>
        /// Cập nhật thông tin deal
        /// </summary>
        Task<(bool Success, string Message)> UpdateDealAsync(Deal deal, int employeeId);

        /// <summary>
        /// Xóa mềm deal (đánh dấu IsDeleted = true)
        /// </summary>
        Task<(bool Success, string Message)> SoftDeleteAsync(int dealId, int employeeId);

        /// <summary>
        /// Lọc deal theo giai đoạn (Stage)
        /// </summary>
        Task<IEnumerable<Deal>> GetDealsByStageAsync(string stage, int? employeeId = null);

        /// <summary>
        /// Tìm kiếm deal theo từ khóa (tên deal hoặc công ty khách hàng)
        /// </summary>
        Task<IEnumerable<Deal>> SearchDealsAsync(int employeeId, string keyword);

        /// <summary>
        /// Lấy các deal sắp đến hạn trong vòng X ngày
        /// </summary>
        Task<IEnumerable<Deal>> GetDealsNearDeadlineAsync(int employeeId, int daysAhead = 7);

        /// <summary>
        /// Tính tổng giá trị deal theo giai đoạn
        /// </summary>
        Task<decimal> GetTotalDealValueByStageAsync(int employeeId, string stage);

        /// <summary>
        /// Cập nhật giai đoạn (Stage) của deal
        /// </summary>
        Task<bool> UpdateDealStageAsync(int dealId, string newStage, int employeeId);



        // =====================================================================
        // 👔 KHU VỰC MANAGER (DEAL SUPERVISION)
        // =====================================================================

        /// <summary>
        /// Lấy toàn bộ deal trong team của manager
        /// </summary>
        Task<IEnumerable<Deal>> GetTeamDealsAsync(int managerId);

        /// <summary>
        /// Lọc deal trong team theo stage, giá trị hoặc deadline
        /// </summary>
        Task<IEnumerable<Deal>> FilterTeamDealsAsync(
            int managerId,
            string? stage = null,
            decimal? minValue = null,
            decimal? maxValue = null,
            DateTime? deadlineBefore = null);

        /// <summary>
        /// Lấy pipeline tổng hợp (tổng giá trị deal theo stage) của team
        /// </summary>
        Task<IDictionary<string, decimal>> GetTeamPipelineSummaryAsync(int managerId);

        /// <summary>
        /// Chuyển giao deal cho nhân viên khác trong team
        /// </summary>
        Task<(bool Success, string Message)> ReassignDealAsync(int dealId, int newEmployeeId, int managerId);

        /// <summary>
        /// Lấy danh sách file hợp đồng/tài liệu của một deal
        /// </summary>
        Task<IEnumerable<string>> GetDealFilesAsync(int dealId);
    }
}

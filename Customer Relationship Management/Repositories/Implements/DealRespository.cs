using Customer_Relationship_Management.Data;
using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Customer_Relationship_Management.Repositories.Implements
{
    /// <summary>
    /// Repository thao tác dữ liệu Deal (Cơ hội kinh doanh)
    /// </summary>
    public class DealRepository : GenericRepository<Deal>, IDealRepository
    {
        private readonly B2BDbContext _context;

        public DealRepository(B2BDbContext context) : base(context)
        {
            _context = context;
        }

        // =====================================================================
        // 🧍‍♂️ KHU VỰC NHÂN VIÊN
        // =====================================================================

        /// <summary>
        /// Lấy tất cả deal của nhân viên
        /// </summary>
        public async Task<IEnumerable<Deal>> GetDealsByEmployeeAsync(int employeeId)
        {
            return await _context.Deals
                .Include(d => d.Customer)
                .Where(d => !d.IsDeleted &&
                            (
                                // Nếu là người đang phụ trách hiện tại
                                d.Customer.AssignedToUserID == employeeId
                                // Hoặc là người tạo, nhưng khách hàng chưa được giao cho ai khác
                                || (d.CreatedByUserID == employeeId &&
                                    (d.Customer.AssignedToUserID == null || d.Customer.AssignedToUserID == employeeId))
                            ))
                .OrderByDescending(d => d.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }


        /// <summary>
        /// Lấy deal theo ID có kèm thông tin khách hàng
        /// </summary>
        public async Task<Deal?> GetDealWithCustomerByIdAsync(int dealId)
        {
            return await _context.Deals
                .Include(d => d.Customer)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DealID == dealId);
        }

        /// <summary>
        /// Lọc deal theo giai đoạn (Stage)
        /// </summary>
        public async Task<IEnumerable<Deal>> GetDealsByStageAsync(string stage, int? employeeId = null)
        {
            var query = _context.Deals
                .Include(d => d.Customer)
                .Where(d => d.Stage == stage && !d.IsDeleted);

            if (employeeId.HasValue)
                query = query.Where(d => d.Customer.AssignedToUserID == employeeId.Value);

            return await query
                .OrderByDescending(d => d.UpdatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Xóa mềm (đánh dấu IsDeleted = true)
        /// </summary>
        public async Task SoftDeleteAsync(int dealId, int currentUserId)
        {
            var deal = await _context.Deals.FirstOrDefaultAsync(d => d.DealID == dealId);
            if (deal == null)
                return;

            if (deal.Customer.AssignedToUserID != currentUserId)
                throw new UnauthorizedAccessException("Bạn không có quyền xóa deal này.");

            deal.IsDeleted = true;
            deal.UpdatedAt = DateTime.UtcNow;

            _context.Deals.Update(deal);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Tìm kiếm deal theo tên hoặc công ty khách hàng
        /// </summary>
        public async Task<IEnumerable<Deal>> SearchDealsAsync(int employeeId, string keyword)
        {
            keyword = keyword.ToLower().Trim();

            return await _context.Deals
                .Include(d => d.Customer)
                .Where(d => d.Customer.AssignedToUserID == employeeId
                            && !d.IsDeleted
                            && (d.DealName.ToLower().Contains(keyword)
                                || d.Customer.CompanyName.ToLower().Contains(keyword)))
                .OrderByDescending(d => d.UpdatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Lấy các deal sắp đến hạn trong vòng X ngày
        /// </summary>
        public async Task<IEnumerable<Deal>> GetDealsNearDeadlineAsync(int employeeId, int daysAhead = 7)
        {
            var now = DateTime.UtcNow;
            var upcoming = now.AddDays(daysAhead);

            return await _context.Deals
                .Include(d => d.Customer)
                .Where(d => d.Customer.AssignedToUserID == employeeId
                            && !d.IsDeleted
                            && d.Deadline != null
                            && d.Deadline >= now
                            && d.Deadline <= upcoming)
                .OrderBy(d => d.Deadline)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Tính tổng giá trị deal theo giai đoạn
        /// </summary>
        public async Task<decimal> GetTotalDealValueByStageAsync(int employeeId, string stage)
        {
            return await _context.Deals
                .Where(d => d.Customer.AssignedToUserID == employeeId && !d.IsDeleted && d.Stage == stage)
                .SumAsync(d => (decimal?)d.Value ?? 0);
        }

        /// <summary>
        /// Cập nhật giai đoạn (Stage) của Deal
        /// </summary>
        public async Task<bool> UpdateDealStageAsync(int dealId, string newStage)
        {
            var deal = await _context.Deals
                .Include(d => d.Customer)
                .FirstOrDefaultAsync(d => d.DealID == dealId && !d.IsDeleted);

            if (deal == null)
                return false;

            deal.Stage = newStage;
            deal.UpdatedAt = DateTime.UtcNow;

            _context.Deals.Update(deal);
            await _context.SaveChangesAsync();
            return true;
        }

        // =====================================================================
        // 👔 KHU VỰC MANAGER
        // =====================================================================

        /// <summary>
        /// Lấy tất cả deal của team mà manager quản lý
        /// </summary>
        public async Task<IEnumerable<Deal>> GetTeamDealsAsync(int managerId)
        {
            var employeeIds = await _context.Users
                .Where(u => u.ManagerID == managerId)
                .Select(u => u.UserID)
                .ToListAsync();

            return await _context.Deals
                .Include(d => d.Customer)
                .Where(d => !d.IsDeleted &&
                            d.Customer.AssignedToUserID.HasValue &&
                            employeeIds.Contains(d.Customer.AssignedToUserID.Value))
                .OrderByDescending(d => d.UpdatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Lọc deal của team theo stage, giá trị hoặc deadline
        /// </summary>
        public async Task<IEnumerable<Deal>> FilterTeamDealsAsync(
            int managerId,
            string? stage = null,
            decimal? minValue = null,
            decimal? maxValue = null,
            DateTime? deadlineBefore = null)
        {
            var employeeIds = await _context.Users
                .Where(u => u.ManagerID == managerId)
                .Select(u => u.UserID)
                .ToListAsync();

            var query = _context.Deals
                .Include(d => d.Customer)
                .Where(d => !d.IsDeleted &&
                            d.Customer.AssignedToUserID.HasValue &&
                            employeeIds.Contains(d.Customer.AssignedToUserID.Value));

            if (!string.IsNullOrEmpty(stage))
                query = query.Where(d => d.Stage == stage);

            if (minValue.HasValue)
                query = query.Where(d => d.Value >= minValue.Value);

            if (maxValue.HasValue)
                query = query.Where(d => d.Value <= maxValue.Value);

            if (deadlineBefore.HasValue)
                query = query.Where(d => d.Deadline != null && d.Deadline <= deadlineBefore.Value);

            return await query
                .OrderByDescending(d => d.UpdatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Thống kê pipeline toàn team theo stage
        /// </summary>
        public async Task<IDictionary<string, decimal>> GetTeamPipelineSummaryAsync(int managerId)
        {
            var employeeIds = await _context.Users
                .Where(u => u.ManagerID == managerId)
                .Select(u => u.UserID)
                .ToListAsync();

            return await _context.Deals
                .Where(d => !d.IsDeleted &&
                            d.Customer.AssignedToUserID.HasValue &&
                            employeeIds.Contains(d.Customer.AssignedToUserID.Value))
                .GroupBy(d => d.Stage)
                .Select(g => new { Stage = g.Key, TotalValue = g.Sum(d => (decimal?)d.Value ?? 0) })
                .ToDictionaryAsync(g => g.Stage, g => g.TotalValue);
        }

        /// <summary>
        /// Chuyển giao deal cho nhân viên khác trong team
        /// </summary>
        public async Task<bool> ReassignDealAsync(int dealId, int newEmployeeId, int managerId)
        {
            // Lấy danh sách nhân viên trong team
            var teamMemberIds = await _context.Users
                .Where(u => u.ManagerID == managerId)
                .Select(u => u.UserID)
                .ToListAsync();

            // Kiểm tra quyền: chỉ được gán cho nhân viên cùng team
            if (!teamMemberIds.Contains(newEmployeeId))
                throw new UnauthorizedAccessException("Nhân viên được gán không thuộc quyền quản lý của bạn.");

            // Lấy deal
            var deal = await _context.Deals
                .Include(d => d.Customer)
                .FirstOrDefaultAsync(d => d.DealID == dealId && !d.IsDeleted);

            if (deal == null)
                return false;

            // Kiểm tra xem deal có thuộc quyền quản lý không
            if (!deal.Customer.AssignedToUserID.HasValue ||
                !teamMemberIds.Contains(deal.Customer.AssignedToUserID.Value))
                throw new UnauthorizedAccessException("Bạn không thể chuyển giao deal ngoài quyền quản lý.");

            // Cập nhật người phụ trách mới
            deal.Customer.AssignedToUserID = newEmployeeId;
            deal.UpdatedAt = DateTime.UtcNow;

            _context.Deals.Update(deal);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Lấy danh sách file hợp đồng/tài liệu thuộc deal
        /// </summary>
        public async Task<IEnumerable<string>> GetDealFilesAsync(int dealId)
        {
            return await _context.Contracts
                .Where(c => c.DealID == dealId && !string.IsNullOrEmpty(c.FilePath))
                .Select(c => c.FilePath!)
                .ToListAsync();
        }
    }
}

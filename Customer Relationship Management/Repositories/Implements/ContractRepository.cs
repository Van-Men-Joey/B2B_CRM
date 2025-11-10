using Customer_Relationship_Management.Data;
using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Customer_Relationship_Management.Repositories.Implements
{
    public class ContractRepository : GenericRepository<Contract>, IContractRepository
    {
        public ContractRepository(B2BDbContext context) : base(context) { }

        public async Task<IEnumerable<Contract>> GetByUserAsync(int userId)
        {
            return await _context.Contracts
                .Include(c => c.Deal)
                .Include(c => c.CreatedBy)
                .Where(c => c.CreatedByUserID == userId && !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }
        public async Task<IEnumerable<Contract>> GetByManagerAsync(int managerId)
        {
            return await _context.Contracts
                .Include(c => c.Deal)
                    .ThenInclude(d => d.Customer)
                .Include(c => c.CreatedBy)
                .Where(c => c.ApprovalStatus == "Pending" && !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }
        public async Task<IEnumerable<Contract>> GetPendingContractsAsync()
        {
           return await _context.Contracts
               .Include(c => c.Deal)
               .Include(c => c.CreatedBy)
               .Where(c => c.ApprovalStatus == "Pending" && !c.IsDeleted)
               .OrderByDescending(c => c.CreatedAt)
               .ToListAsync();
        }
        public async Task<IEnumerable<Contract>> GetByStatusAsync(string status)
        {
            return await _context.Contracts
                .Include(c => c.Deal)
                .ThenInclude(d => d.Customer)
                .Include(c => c.CreatedBy)
                .Where(c => c.ApprovalStatus == status && !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }
    }
}

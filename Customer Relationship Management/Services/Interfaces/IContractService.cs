using Customer_Relationship_Management.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Customer_Relationship_Management.Services.Interfaces
{
    public interface IContractService
    {
        Task<IEnumerable<Contract>> GetByUserAsync(int userId);
        Task<IEnumerable<Contract>> GetPendingContractsAsync();
        Task<IEnumerable<Contract>> GetByManagerAsync(int managerId);
        Task<IEnumerable<Contract>> GetByStatusAsync(string status);
        Task<Contract?> GetByIdAsync(int id);

        // Create / Update / Delete with currentUserId để log Audit
        Task CreateAsync(Contract contract, int? currentUserId = null);
        Task UpdateAsync(Contract contract, int? currentUserId = null);
        Task DeleteAsync(int id, int? currentUserId = null);

        // Ví dụ: approve/reject contract
        Task ApproveAsync(int id, string newStatus, int? currentUserId = null);
    }
}
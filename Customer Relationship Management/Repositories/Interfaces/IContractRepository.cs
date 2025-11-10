using Customer_Relationship_Management.Models;

namespace Customer_Relationship_Management.Repositories.Interfaces
{
    public interface IContractRepository : IGenericRepository<Contract>
    {
        Task<IEnumerable<Contract>> GetByUserAsync(int userId);
        Task<IEnumerable<Contract>> GetPendingContractsAsync();
        Task<IEnumerable<Contract>> GetByManagerAsync(int managerId);
        Task<IEnumerable<Contract>> GetByStatusAsync(string status);    
    }
}

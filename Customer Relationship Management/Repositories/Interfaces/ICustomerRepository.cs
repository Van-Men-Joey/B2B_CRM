using Customer_Relationship_Management.Models;

namespace Customer_Relationship_Management.Repositories.Interfaces
{
    public interface ICustomerRepository : IGenericRepository<Customer>
    {
        Task<IEnumerable<Customer>> GetByAssignedUserAsync(int userId);
        Task<Customer?> GetByIdWithRelationsAsync(int id);



        // ⭐ Manager: lấy khách của team dưới quyền
        Task<IEnumerable<Customer>> GetByTeamUsersAsync(IEnumerable<int> teamUserIds);

        // ⭐ Manager: reassign khách
        Task<bool> ReassignCustomerAsync(int customerId, int newUserId);

        // ⭐ Manager: toggle VIP
        Task<bool> UpdateVIPStatusAsync(int customerId, bool isVIP);
    }
}

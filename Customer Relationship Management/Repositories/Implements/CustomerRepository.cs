using Customer_Relationship_Management.Data;
using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Customer_Relationship_Management.Repositories.Implements
{
    public class CustomerRepository : GenericRepository<Customer>, ICustomerRepository
    {
        private readonly B2BDbContext _context;

        public CustomerRepository(B2BDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Customer>> GetByAssignedUserAsync(int userId)
        {
            return await _context.Customers
                .Where(c => c.AssignedToUserID == userId && !c.IsDeleted)
                .Include(c => c.AssignedUser)
                .Include(c => c.Deals)
                .ToListAsync();
        }

        public async Task<Customer?> GetByIdWithRelationsAsync(int id)
        {
            return await _context.Customers
                .Where(c => c.CustomerID == id && !c.IsDeleted)
                .Include(c => c.AssignedUser)
                .Include(c => c.Deals)
                .FirstOrDefaultAsync();
        }
        public async Task<IEnumerable<Customer>> GetByTeamUsersAsync(IEnumerable<int> teamUserIds)
        {
            return await _context.Customers
                .Where(c => teamUserIds.Contains(c.AssignedUser.ManagerID.Value) && !c.IsDeleted)
                .Include(c => c.AssignedUser)
                .Include(c => c.Deals)
                .ToListAsync();
        }

        public async Task<bool> ReassignCustomerAsync(int customerId, int newUserId)
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerID == customerId && !c.IsDeleted);
            if (customer == null) return false;

            customer.AssignedToUserID = newUserId;
            customer.UpdatedAt = DateTime.UtcNow;

            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateVIPStatusAsync(int customerId, bool isVIP)
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerID == customerId && !c.IsDeleted);
            if (customer == null) return false;

            customer.VIP = isVIP;
            customer.UpdatedAt = DateTime.UtcNow;

            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();
            return true;
        }
    }

}

using Customer_Relationship_Management.Data;
using Customer_Relationship_Management.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Customer_Relationship_Management.Models;

namespace Customer_Relationship_Management.Repositories.Implements
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class, IEntity
    {
        protected readonly B2BDbContext _context;
        private readonly DbSet<T> _dbSet;

        public GenericRepository(B2BDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<T?> GetByIdAsync(object id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public void Update(T entity)
        {
            _dbSet.Update(entity);
        }
        public async Task UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
        }


        public void Delete(T entity)
        {
            _dbSet.Remove(entity);
        }

        public async Task DeleteAsync(T entity)
        {
            _dbSet.Remove(entity);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }


        // NoTracking để tăng hiệu suất khi chỉ đọc dữ liệu, không cần theo dõi thay đổi.
        //Sử dụng cho CustomerService, DealService,... khi cần log dữ liệu cũ trước khi cập nhật.
        public virtual async Task<T?> GetByIdAsNoTrackingAsync(int id)
        {
            var entityType = _context.Model.FindEntityType(typeof(T));
            var primaryKey = entityType?.FindPrimaryKey()?.Properties.FirstOrDefault();

            if (primaryKey == null)
                throw new InvalidOperationException($"Entity {typeof(T).Name} không có khóa chính.");

            var keyName = primaryKey.Name; // ví dụ "CustomerID", "DealID"
            return await _dbSet.AsNoTracking()
                               .FirstOrDefaultAsync(e => EF.Property<int>(e, keyName) == id);
        }



    }
}

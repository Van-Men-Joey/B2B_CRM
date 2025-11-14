using Customer_Relationship_Management.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Customer_Relationship_Management.Repositories.Interfaces
{

    public interface IGenericRepository<T> where T : class
    {
        //Lấy tất cả bản ghi của entity T (User, Role…) từ DB (bất đồng bộ).
        Task<IEnumerable<T>> GetAllAsync();


        //Lấy một bản ghi theo ID (khóa chính) từ DB (bất đồng bộ).object để bạn có thể truyền int, Guid
        Task<T?> GetByIdAsync(object id);


        //Tìm kiếm các bản ghi theo điều kiện (predicate) từ DB (bất đồng bộ). Predicate là biểu thức Lambda dùng để lọc dữ liệu.
        // ví dụ await userRepo.FindAsync(u => u.Status == "Active");
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<T?> GetByIdAsNoTrackingAsync(int id);
        Task<T?> GetByIdAsNoTrackingAsync(int id, params Expression<Func<T, object>>[] includes);

        Task AddAsync(T entity);
        void Update(T entity);

        Task UpdateAsync(T entity);
        void Delete(T entity);
        Task DeleteAsync(T entity);   // ✅ PHẢI có Async

        //Lưu các thay đổi (Add/Update/Delete) xuống DB.
        Task SaveChangesAsync();
    }

    // ý NGHĨA: Tách biệt logic truy xuất dữ liệu khỏi Controller/Service (SRP – Single Responsibility).

    //Giảm lặp code CRUD.

    //Tuân thủ DIP: Controller/Service chỉ làm việc với interface, không phụ thuộc vào EF Core trực tiếp → dễ test/mock sau này.
}


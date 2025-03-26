using Web.Models;
using System.Collections.Generic;
namespace Web.Reponsitory
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<Product> GetByIdAsync(int id);
        Task AddAsync(Product product);
        Task UpdateAsync(Product product);
        Task DeleteAsync(int id);
        Task<bool> DeleteProductImageAsync(int imageId);
    }
}

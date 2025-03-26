using Web.Models;
using System.Collections.Generic;
namespace Web.Reponsitory
    {
        public interface ICategoryRepository
        {
            Task<IEnumerable<Category>> GetAllAsync();
            Task<Category> GetByIdAsync(int id);
            Task AddAsync(Category category);
            Task UpdateAsync(Category category);
            Task DeleteAsync(int id);
    }
    }

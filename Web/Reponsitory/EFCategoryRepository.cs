using Microsoft.EntityFrameworkCore;
using Web.Models;

namespace Web.Reponsitory
{
    public class EFCategoryRepository : ICategoryRepository
    {
        private readonly ApplicationDbContext _context;
        public EFCategoryRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<Category>> GetAllAsync()
        {
            return await _context.Categories
                .ToListAsync();
        }
        public async Task<Category> GetByIdAsync(int id)
        {
            return await _context.Categories.FirstOrDefaultAsync(p => p.Id == id);
        }
        public async Task AddAsync(Category Category)
        {
            _context.Categories.Add(Category);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateAsync(Category Category)
        {
            _context.Categories.Update(Category);
            await _context.SaveChangesAsync();
        }
        public async Task DeleteAsync(int id)
        {
            var Category = await _context.Categories.FindAsync(id);
            _context.Categories.Remove(Category);
            await _context.SaveChangesAsync();
        }

    }
}

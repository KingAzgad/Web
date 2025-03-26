using Microsoft.EntityFrameworkCore;
using Web.Models;

namespace Web.Reponsitory
{
    public class EFProductRepository : IProductRepository   
    {
        private readonly ApplicationDbContext _context;
        public EFProductRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ImageUrls)
                .ToListAsync();
        }
        public async Task<Product> GetByIdAsync(int id)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ImageUrls)
                .FirstOrDefaultAsync(p => p.Id == id);
        }
        public async Task AddAsync(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateAsync(Product product)
        {
            if (product.ImageUrls != null && product.ImageUrls.Any())
            {
                foreach (var image in product.ImageUrls)
                {
                    if (image.Id == 0) // Nếu là hình ảnh mới
                    {
                        _context.Entry(image).State = EntityState.Added;
                    }
                }
            }

            _context.Entry(product).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }
        public async Task DeleteAsync(int id)
        {
            var product = await _context.Products
                .Include(p => p.ImageUrls)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product != null)
            {
                // Xóa các hình ảnh phụ
                if (product.ImageUrls != null && product.ImageUrls.Any())
                {
                    _context.RemoveRange(product.ImageUrls);
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<bool> DeleteProductImageAsync(int imageId)
        {
            var image = await _context.Set<ProductImage>().FindAsync(imageId);
            if (image != null)
            {
                _context.Set<ProductImage>().Remove(image);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

    }
}

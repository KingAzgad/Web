using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Web.Models;
using Web.Reponsitory;

namespace Web.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IProductRepository _productRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(ICategoryRepository categoryRepository, IProductRepository productRepository, IWebHostEnvironment webHostEnvironment)
        {
            _categoryRepository = categoryRepository;
            _productRepository = productRepository;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _productRepository.GetAllAsync();
            return View(products);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddAsync()
        {
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Add(Product product, IFormFile? mainImageFile = null, List<IFormFile>? imageFiles = null)
        {
            if (ModelState.IsValid)
            {
                // Xử lý hình ảnh chính
                if (mainImageFile != null && mainImageFile.Length > 0)
                {
                    product.ImageUrl = await SaveImage(mainImageFile);
                }

                // Thêm sản phẩm để lấy Id
                await _productRepository.AddAsync(product);

                // Xử lý các hình ảnh phụ
                if (imageFiles != null && imageFiles.Count > 0)
                {
                    product.ImageUrls = new List<ProductImage>();
                    foreach (var file in imageFiles)
                    {
                        if (file.Length > 0)
                        {
                            var imageUrl = await SaveImage(file);
                            var productImage = new ProductImage
                            {
                                Url = imageUrl,
                                ProductId = product.Id
                            };
                            product.ImageUrls.Add(productImage);
                        }
                    }
                    // Cập nhật sản phẩm với các hình ảnh mới
                    await _productRepository.UpdateAsync(product);
                }

                return RedirectToAction(nameof(Index));
            }

            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View(product);
        }

        public async Task<IActionResult> Display(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, Product product, IFormFile? mainImageFile = null, List<IFormFile>? additionalImageFiles = null)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Lấy sản phẩm từ cơ sở dữ liệu để có thông tin đầy đủ
                var existingProduct = await _productRepository.GetByIdAsync(id);
                if (existingProduct == null)
                {
                    return NotFound();
                }

                // Cập nhật thông tin cơ bản
                existingProduct.Name = product.Name;
                existingProduct.Description = product.Description;
                existingProduct.Price = product.Price;
                existingProduct.CategoryId = product.CategoryId;

                // Xử lý hình ảnh chính
                if (mainImageFile != null && mainImageFile.Length > 0)
                {
                    // Xóa file cũ nếu có
                    if (!string.IsNullOrEmpty(existingProduct.ImageUrl))
                    {
                        DeleteImage(existingProduct.ImageUrl);
                    }
                    existingProduct.ImageUrl = await SaveImage(mainImageFile);
                }

                // Xử lý các hình ảnh phụ
                if (additionalImageFiles != null && additionalImageFiles.Count > 0)
                {
                    if (existingProduct.ImageUrls == null)
                    {
                        existingProduct.ImageUrls = new List<ProductImage>();
                    }

                    foreach (var file in additionalImageFiles)
                    {
                        if (file.Length > 0)
                        {
                            var imageUrl = await SaveImage(file);
                            var productImage = new ProductImage
                            {
                                Url = imageUrl,
                                ProductId = existingProduct.Id
                            };
                            existingProduct.ImageUrls.Add(productImage);
                        }
                    }
                }

                // Cập nhật sản phẩm
                await _productRepository.UpdateAsync(existingProduct);
                return RedirectToAction(nameof(Index));
            }

            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product != null)
            {
                // Xóa hình ảnh chính
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    DeleteImage(product.ImageUrl);
                }

                // Xóa các hình ảnh phụ
                if (product.ImageUrls != null && product.ImageUrls.Count > 0)
                {
                    foreach (var image in product.ImageUrls)
                    {
                        DeleteImage(image.Url);
                    }
                }
            }

            await _productRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteImage(int id, int productId)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null || product.ImageUrls == null)
            {
                return NotFound();
            }

            var imageToDelete = product.ImageUrls.FirstOrDefault(i => i.Id == id);
            if (imageToDelete != null)
            {
                DeleteImage(imageToDelete.Url);
                product.ImageUrls.Remove(imageToDelete);
                await _productRepository.UpdateAsync(product);
            }

            return RedirectToAction(nameof(Update), new { id = productId });
        }

        private async Task<string> SaveImage(IFormFile image)
        {
            if (image == null || image.Length == 0)
                return null;

            // Tạo tên file độc nhất để tránh trùng lặp
            string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(image.FileName)}";

            // Đường dẫn đầy đủ đến thư mục lưu trữ
            string uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "images");

            // Tạo thư mục nếu chưa tồn tại
            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }

            // Đường dẫn đầy đủ đến file
            string filePath = Path.Combine(uploadDir, uniqueFileName);

            // Lưu file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            // Trả về đường dẫn tương đối để lưu vào cơ sở dữ liệu
            return $"/images/{uniqueFileName}";
        }

        private void DeleteImage(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return;

            try
            {
                // Chuyển đổi đường dẫn tương đối thành đường dẫn tuyệt đối
                string fileName = Path.GetFileName(imageUrl);
                string filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", fileName);

                // Kiểm tra xem file có tồn tại không
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                // Xử lý ngoại lệ nếu cần
                Console.WriteLine($"Lỗi khi xóa file: {ex.Message}");
            }
        }
    }
}
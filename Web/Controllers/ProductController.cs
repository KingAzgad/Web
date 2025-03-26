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

                if (mainImageFile != null && mainImageFile.Length > 0)
                {
                    product.ImageUrl = await SaveImage(mainImageFile);
                }


                await _productRepository.AddAsync(product);


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

                var existingProduct = await _productRepository.GetByIdAsync(id);
                if (existingProduct == null)
                {
                    return NotFound();
                }

                existingProduct.Name = product.Name;
                existingProduct.Description = product.Description;
                existingProduct.Price = product.Price;
                existingProduct.CategoryId = product.CategoryId;

                if (mainImageFile != null && mainImageFile.Length > 0)
                {
                    if (!string.IsNullOrEmpty(existingProduct.ImageUrl))
                    {
                        DeleteImage(existingProduct.ImageUrl);
                    }
                    existingProduct.ImageUrl = await SaveImage(mainImageFile);
                }

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
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    DeleteImage(product.ImageUrl);
                }

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

            string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(image.FileName)}";

            string uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "images");

            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }

            string filePath = Path.Combine(uploadDir, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            return $"/images/{uniqueFileName}";
        }

        private void DeleteImage(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return;

            try
            {
                string fileName = Path.GetFileName(imageUrl);
                string filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", fileName);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi xóa file: {ex.Message}");
            }
        }
    }
}
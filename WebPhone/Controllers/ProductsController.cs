using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using WebPhone.Attributes;
using WebPhone.EF;
using WebPhone.Models.Products;
using WebPhone.Repositories;

namespace WebPhone.Controllers
{
    [RoutePrefix("product")]
    [AppAuthorize("Adminitrator, Manage, Employment")]
    public class ProductsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ProductRepository _productRepository;

        private readonly int ITEM_PER_PAGE = 10;

        public ProductsController(ProductRepository productRepository)
        {
            _context = new AppDbContext();
            _productRepository = productRepository;
        }

        #region CURD Category Product
        [HttpGet]
        [Route("category")]
        public async Task<ActionResult> CateIndex()
        {
            try
            {
                var cateProduct = await _productRepository.GetListCategoryByParentIdAsync(null);
                var items = new List<CategoryProduct>();
                await _productRepository.CreateSelectItem(cateProduct, items, 0);

                return View(items);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                TempData["Message"] = "Error: Lỗi hệ thống";
                return View("Index", "Home");
            }

        }

        [HttpGet]
        [Route("category/details")]
        public async Task<ActionResult> CateDetails(Guid id)
        {
            var categoryProduct = await _productRepository.GetCategoryByIdAsync(id);

            if (categoryProduct == null)
            {
                TempData["Message"] = "Error: Không tìm thấy thông tin";
                return RedirectToAction(nameof(CateIndex));
            }

            return View(categoryProduct);
        }

        [HttpGet]
        [Route("category/create")]
        public async Task<ActionResult> CateCreate()
        {
            ViewBag.SelectList = await _productRepository.RenderCatePoduct();

            return View();
        }

        [HttpPost]
        [Route("category/create")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CateCreate([Bind(Include = "CategoryName, ParentId")] CateProductDTO cateProductDTO)
        {
            ViewBag.SelectList = await _productRepository.RenderCatePoduct();

            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["Message"] = "Error: Vui lòng nhập đầy đủ thông tin";
                    return View(cateProductDTO);
                }

                var result = await _productRepository.CreateCategoryAsync(cateProductDTO);
                if (!result.Succeeded)
                {
                    TempData["Message"] = $"Error: {result.Message}";
                    return View(cateProductDTO);
                }

                TempData["Message"] = "Success: Tạo danh mục thành công";

                return RedirectToAction(nameof(CateIndex));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                TempData["Message"] = "Error: Lỗi hệ thống";
                return RedirectToAction(nameof(CateCreate));
            }
        }

        [HttpGet]
        [Route("category/edit")]
        public async Task<ActionResult> CateEdit(Guid id)
        {
            ViewBag.SelectList = await _productRepository.RenderCatePoduct();

            var categoryProduct = await _productRepository.GetCategoryByIdAsync(id);
            if (categoryProduct == null)
            {
                TempData["Message"] = "Error: Không tìm thấy danh mục sản phẩm";
                return RedirectToAction(nameof(CateIndex));
            }

            var cateProductDTO = new CateProductDTO
            {
                Id = categoryProduct.Id,
                CategoryName = categoryProduct.CategoryName,
                ParentId = categoryProduct.ParentId,
            };

            return View(cateProductDTO);
        }

        [HttpPost]
        [Route("category/edit")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CateEdit(Guid id, [Bind(Include = "Id,CategoryName, ParentId")] CateProductDTO cateProductDTO)
        {
            ViewBag.SelectList = await _productRepository.RenderCatePoduct();

            try
            {
                if (id != cateProductDTO.Id)
                {
                    TempData["Message"] = "Error: Danh mục không hợp lệ";
                    return View(cateProductDTO);
                }

                if (!ModelState.IsValid)
                {
                    TempData["Message"] = "Error: Vui lòng nhập đầy đủ thông tin";
                    return View(cateProductDTO);
                }

                var result = await _productRepository.EditCategoryAsync(cateProductDTO);
                if (!result.Succeeded)
                {
                    TempData["Message"] = $"Error: {result.Message}";
                    return View(cateProductDTO);
                }

                return RedirectToAction(nameof(CateIndex));
            }
            catch (Exception)
            {
                TempData["Message"] = "Error: Lỗi hệ thống";
                return RedirectToAction(nameof(CateEdit));
            }
        }

        [HttpGet]
        [Route("category/delete")]
        public async Task<ActionResult> CateDelete(Guid id)
        {
            var categoryProduct = await _productRepository.GetCategoryByIdAsync(id);

            if (categoryProduct == null)
            {
                TempData["Message"] = "Error: Không tìm thấy thông tin";
                return RedirectToAction(nameof(CateIndex));
            }

            return View(categoryProduct);
        }

        [HttpPost, ActionName("Delete")]
        [Route("category/delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CateDeleteConfirm(Guid id)
        {
            try
            {
                var result = await _productRepository.DeleteCategoryAsync(id);
                if (!result.Succeeded)
                {
                    TempData["Message"] = $"Error: {result.Message}";
                    return RedirectToAction(nameof(CateIndex));
                }

                TempData["Message"] = "Success: Xóa danh mục thành công";
                return RedirectToAction(nameof(CateIndex));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                TempData["Message"] = "Error: Lỗi hệ thống";
                return RedirectToAction(nameof(CateIndex));
            }
        }
        #endregion

        #region CURD Product
        [HttpGet]
        [Route]
        public async Task<ActionResult> Index(string nameProduct, int page = 1)
        {
            try
            {
                nameProduct = nameProduct == null ? "" : nameProduct;

                int total = await _context.Products.Where(p => p.ProductName.Contains(nameProduct)).CountAsync();
                int countPage = (int)Math.Ceiling((double)total / ITEM_PER_PAGE);
                countPage = countPage < 1 ? 1 : countPage;
                page = page > countPage ? countPage : page;
                page = page < 1 ? 1 : page;

                var listProduct = await _productRepository.GetListProductByNameAsync(nameProduct, page, ITEM_PER_PAGE);

                ViewBag.CountPage = countPage;

                return View(listProduct);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                TempData["Message"] = "Error: Lỗi hệ thống";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        [Route("details")]
        public async Task<ActionResult> Details(Guid id)
        {
            var product = await _productRepository.GetProductByIdAsync(id);

            if (product == null)
            {
                TempData["Message"] = "Error: Không tìm thấy dữ liệu";
                return RedirectToAction(nameof(Index));
            }

            return View(product);
        }

        [HttpGet]
        [Route("create")]
        public async Task<ActionResult> Create()
        {
            ViewBag.SelectList = await _productRepository.RenderCatePoduct();

            return View();
        }

        [HttpPost]
        [Route("create")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(ProductDTO productDTO)
        {
            ViewBag.SelectList = await _productRepository.RenderCatePoduct();
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["Message"] = "Error: Vui lòng nhập đầy đủ thông tin";
                    return View(productDTO);
                }

                var result = await _productRepository.CreateAsync(productDTO);
                if(!result.Succeeded)
                {
                    TempData["Message"] = $"Error: {result.Message}";
                    return View(productDTO);
                }

                TempData["Message"] = "Success: Thêm mới sản phẩm thành công";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                TempData["Message"] = "Error: Lỗi hệ thống";
                return RedirectToAction(nameof(Create));
            }
        }

        [HttpGet]
        [Route("edit")]
        public async Task<ActionResult> Edit(Guid id)
        {
            ViewBag.SelectList = await _productRepository.RenderCatePoduct();

            var product = await _productRepository.GetProductByIdAsync(id);
            if (product == null)
            {
                TempData["Message"] = "Error: Không tìm thấy sản phẩm";
                return RedirectToAction(nameof(Index));
            }

            var productDTO = new ProductDTO
            {
                Id = product.Id,
                ProductName = product.ProductName,
                Description = product.Description,
                Price = product.Price,
                Discount = product.Discount,
                CategoryId = product.CategoryId
            };

            return View(productDTO);
        }

        [HttpPost]
        [Route("edit")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, ProductDTO productDTO)
        {
            ViewBag.SelectList = await _productRepository.RenderCatePoduct();

            try
            {
                if (id != productDTO.Id)
                {
                    TempData["Message"] = "Error: Danh mục không hợp lệ";
                    return View(productDTO);
                }

                if (!ModelState.IsValid)
                {
                    TempData["Message"] = "Error: Vui lòng nhập đầy đủ thông tin";
                    return View(productDTO);
                }

                var result = await _productRepository.UpdateAsync(productDTO);
                if (!result.Succeeded)
                {
                    TempData["Message"] = $"Error: {result.Message}";
                    return View(productDTO);
                }

                TempData["Message"] = "Success: Sửa sản phẩm thành công";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                TempData["Message"] = "Error: Lỗi hệ thống";
                return RedirectToAction(nameof(Create));
            }
        }

        [HttpGet]
        [Route("delete")]
        public async Task<ActionResult> Delete(Guid id)
        {
            var product = await _productRepository.GetProductByIdAsync(id);

            if (product == null)
            {
                TempData["Message"] = "Error: Không tìm thấy sản phẩm";
                return RedirectToAction(nameof(Index));
            }

            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [Route("delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirm(Guid id)
        {
            var result = await _productRepository.DeleteAsync(id);
            if(!result.Succeeded)
            {
                TempData["Message"] = $"Error: {result.Message}";
                return View(nameof(Index));
            }

            TempData["Message"] = "Success: Xóa sản phẩm thành công";

            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region Random Product
        [HttpGet]
        [Route("random/{count}")]
        public async Task<ActionResult> RamdomProduct(int count)
        {
            try
            {
                var cateProIdList = await _context.CategoryProducts.Select(cp => cp.Id).ToListAsync();
                Random random = new Random();

                var countName = await _context.Products.CountAsync();

                for (int i = 1; i <= count; i++)
                {
                    var index = random.Next(cateProIdList.Count);
                    var cateProId = cateProIdList[index];
                    var price = random.Next(5000000, 50000000);
                    var discount = random.Next((price / 2), price);

                    var product = new Product
                    {
                        ProductName = $"Sản phẩm {++countName}",
                        //Avatar = $"https://picsum.photos/100/50?random={countName}",
                        Description = $"Thông tin sản phẩm {i}",
                        Price = (int)Math.Round((double)price / 1000) * 1000,
                        Discount = (int)Math.Round((double)discount / 1000) * 1000,
                        CategoryId = cateProId,
                    };

                    _context.Products.Add(product);
                }
                await _context.SaveChangesAsync();

                TempData["Message"] = "Success: Thêm sản phẩm thành công";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                if (ex.InnerException is SqlException sqlEx)
                {
                    // 2601: Cannot insert duplicate key row
                    // 2627: Violation of UNIQUE KEY constraint
                    if (sqlEx.Number == 2601 || sqlEx.Number == 2627)
                    {
                        TempData["Message"] = "Error: Có sản phẩm bị trùng tên";
                        return RedirectToAction(nameof(Index));
                    }
                }

                TempData["Message"] = "Error: Lỗi hệ thống";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        [Route("random/delete/{count}")]
        public async Task<ActionResult> DeleteProduct(int count)
        {
            try
            {
                var products = await _context.Products.Take(count).ToListAsync();

                _context.Products.RemoveRange(products);

                await _context.SaveChangesAsync();

                TempData["Message"] = "Success: Xóa sản phẩm thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["Message"] = "Error: Lỗi hệ thống";
                return RedirectToAction(nameof(Index));
            }
        }
        #endregion

        [HttpPost]
        [Route("filter")]
        public async Task<JsonResult> FilterProduct(string name)
        {
            try
            {
                name = string.IsNullOrEmpty(name) ? "" : name;

                var products = await _context.Products
                                .Where(p => p.ProductName.Contains(name))
                                .Take(100)
                                .ToListAsync();

                var listProduct = products.Select(p => new Product
                {
                    Id = p.Id,
                    ProductName = p.ProductName,
                    Price = p.Price,
                    Discount = p.Discount,
                }).ToList();

                return Json(new
                {
                    Success = true,
                    Message = "Success",
                    Data = listProduct
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    Success = false,
                    Message = ex.Message,
                });
            }
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose(); // Giải phóng DbContext
            }
            base.Dispose(disposing);
        }
    }
}
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
using WebPhone.EF;
using WebPhone.Models.Products;

namespace WebPhone.Controllers
{
    [RoutePrefix("product")]
    public class ProductsController : Controller
    {
        private readonly AppDbContext _context;

        private readonly int ITEM_PER_PAGE = 10;

        public ProductsController()
        {
            _context = new AppDbContext();
        }

        #region CURD Category Product
        [HttpGet]
        [Route("category")]
        public async Task<ActionResult> CateIndex()
        {
            var cateProduct = await _context.CategoryProducts.Where(cp => cp.ParentId == null).ToListAsync();
            await GetCateChildren(cateProduct);

            var items = new List<CategoryProduct>();
            CreateSelectItem(cateProduct, items, 0);

            return View(items);
        }

        [HttpGet]
        [Route("category/details")]
        public async Task<ActionResult> CateDetails(Guid? id)
        {
            var categoryProduct = await _context.CategoryProducts
                .FirstOrDefaultAsync(m => m.Id == id);
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
            await RenderCatePoduct();

            return View();
        }

        [HttpPost]
        [Route("category/create")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CateCreate([Bind(Include = "CategoryName, ParentId")] CateProductDTO cateProductDTO)
        {
            await RenderCatePoduct();

            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["Message"] = "Error: Vui lòng nhập đầy đủ thông tin";
                    return View(cateProductDTO);
                }

                var categoryProduct = new CategoryProduct
                {
                    CategoryName = cateProductDTO.CategoryName,
                    ParentId = cateProductDTO.ParentId
                };

                _context.CategoryProducts.Add(categoryProduct);
                await _context.SaveChangesAsync();

                TempData["Message"] = "Success: Tạo danh mục thành công";

                return RedirectToAction(nameof(CateIndex));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException?.Message);
                TempData["Message"] = "Error: Lỗi hệ thống";
                return RedirectToAction(nameof(CateCreate));
            }
        }

        [HttpGet]
        [Route("category/edit")]
        public async Task<ActionResult> CateEdit(Guid? id)
        {
            await RenderCatePoduct();

            var categoryProduct = await _context.CategoryProducts.FindAsync(id);
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
            await RenderCatePoduct();

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

                var categoryProduct = await _context.CategoryProducts.FindAsync(id);
                if (categoryProduct == null)
                {
                    TempData["Message"] = "Error: Không tìm thấy danh mục";
                    return View(cateProductDTO);
                }

                categoryProduct.CategoryName = cateProductDTO.CategoryName;
                categoryProduct.ParentId = cateProductDTO.ParentId;
                categoryProduct.UpdateAt = DateTime.Now;

                //_context.CategoryProducts.Update(categoryProduct);
                await _context.SaveChangesAsync();

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
        public async Task<ActionResult> CateDelete(Guid? id)
        {
            var categoryProduct = await _context.CategoryProducts
                .FirstOrDefaultAsync(m => m.Id == id);
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
        public async Task<ActionResult> CateDelete(Guid id)
        {
            try
            {
                var categoryProduct = await _context.CategoryProducts.FindAsync(id);
                if (categoryProduct == null)
                {
                    TempData["Message"] = "Error: Không tìm thấy danh mục sản phẩm";
                    return RedirectToAction(nameof(CateIndex));
                }

                var cateProductChildren = await _context.CategoryProducts.Where(cp => cp.ParentId == categoryProduct.Id).ToListAsync();
                if (cateProductChildren.Count > 0)
                {
                    TempData["Message"] = "Error: Không thể xóa danh mục có chứa danh mục con";
                    return RedirectToAction(nameof(CateIndex));
                }

                _context.CategoryProducts.Remove(categoryProduct);

                await _context.SaveChangesAsync();

                TempData["Message"] = "Success: Xóa danh mục thành công";
                return RedirectToAction(nameof(CateIndex));
            }
            catch (Exception)
            {
                TempData["Message"] = "Error: Lỗi hệ thống";
                return RedirectToAction(nameof(CateDelete));
            }
        }
        #endregion

        #region CURD Product
        [HttpGet]
        [Route]
        public async Task<ActionResult> Index(string nameProduct = null, int page = 1)
        {
            try
            {
                var listProduct = new List<Product>();
                //var listProductCache = await GetProductInCache();

                // Nếu có truyền name product
                if (!string.IsNullOrEmpty(nameProduct))
                {
                    listProduct = await (from p in _context.Products
                                         where p.ProductName.Contains(nameProduct)
                                         orderby p.Price ascending
                                         select p).ToListAsync();

                    //listProduct = listProductCache.OrderBy(p => p.Price)
                    //                .Where(p => p.ProductName.Contains(nameProduct))
                    //                .ToList();
                }
                else
                {
                    listProduct = await (from product in _context.Products
                                         orderby product.Price ascending
                                         select product).ToListAsync();

                    //listProduct = listProductCache.OrderByDescending(p => p.Price).ToList();
                }

                // Lấy tổng số lượng sản phẩm
                var total = listProduct.Count();
                // Chia ra số trang theo số lượng hiện thị sản phẩm trên mỗi trang
                var countPage = (int)Math.Ceiling((double)total / ITEM_PER_PAGE);
                countPage = countPage < 1 ? 1 : countPage;
                ViewBag.CountPage = countPage;
                // Nếu page truyền vào > số trang thì lấy số trang
                page = page < 1 ? 1 : page;
                page = page > countPage ? countPage : page;

                listProduct = listProduct.Skip((page - 1) * ITEM_PER_PAGE).Take(ITEM_PER_PAGE).ToList();

                return View(listProduct);
            }
            catch (Exception)
            {
                TempData["Message"] = "Error: Lỗi hệ thống";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        [Route("details")]
        public async Task<ActionResult> Details(Guid? id)
        {
            var product = await _context.Products
                .Include(p => p.CategoryProduct)
                .Select(p => new Product
                {
                    Id = p.Id,
                    ProductName = p.ProductName,
                    Description = p.Description,
                    Price = p.Price,
                    Discount = p.Discount,
                    CreateAt = p.CreateAt,
                    UpdateAt = p.UpdateAt,
                    CategoryProduct = p.CategoryProduct,
                }).FirstOrDefaultAsync(m => m.Id == id);

            var productS = await _context.Products
                .Include(p => p.CategoryProduct)
                .FirstOrDefaultAsync(m => m.Id == id);
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
            await RenderCatePoduct();

            return View();
        }

        [HttpPost]
        [Route("create")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(ProductDTO productDTO)
        {
            await RenderCatePoduct();
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["Message"] = "Error: Vui lòng nhập đầy đủ thông tin";
                    return View(productDTO);
                }

                var product = new Product
                {
                    ProductName = productDTO.ProductName,
                    Description = productDTO.Description,
                    Price = (int)Math.Round((double)productDTO.Price / 1000) * 1000,
                    Discount = (int)Math.Round((double)(productDTO.Discount ?? 0) / 1000) * 1000,
                    CategoryId = productDTO.CategoryId
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                TempData["Message"] = "Success: Thêm mới sản phẩm thành công";

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
                        TempData["Message"] = "Error: Tên sản phẩm đã được sử dụng";
                        return View(productDTO);
                    }
                }

                TempData["Message"] = "Error: Lỗi hệ thống";
                return RedirectToAction(nameof(Create));
            }
        }

        [HttpGet]
        [Route("edit")]
        public async Task<ActionResult> Edit(Guid? id)
        {
            await RenderCatePoduct();

            var product = await _context.Products.FindAsync(id);
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
            await RenderCatePoduct();

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

                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    TempData["Message"] = "Error: Không tìm thấy sản phẩm";
                    return View(productDTO);
                }

                product.ProductName = productDTO.ProductName;
                product.Description = productDTO.Description;
                product.Price = productDTO.Price;
                product.Discount = productDTO.Discount;
                product.CategoryId = productDTO.CategoryId;
                product.UpdateAt = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["Message"] = "Success: Sửa sản phẩm thành công";

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
                        TempData["Message"] = "Error: Tên sản phẩm đã được sử dụng";
                        return View(productDTO);
                    }
                }

                TempData["Message"] = "Error: Lỗi hệ thống";
                return RedirectToAction(nameof(Create));
            }
        }

        [HttpGet]
        [Route("delete")]
        public async Task<ActionResult> Delete(Guid? id)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);

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
        public async Task<ActionResult> Delete(Guid id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                TempData["Message"] = "Error: Không tìm thấy sản phẩm";
                return RedirectToAction(nameof(Delete));
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

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
            name = string.IsNullOrEmpty(name) ? "" : name;

            var products = await _context.Products
                            .Where(p => p.ProductName.Contains(name))
                            .Take(100)
                            .Select(p => new Product
                            {
                                Id = p.Id,
                                ProductName = p.ProductName,
                                Price = p.Price,
                                Discount = p.Discount,
                            })
                            .ToListAsync();

            return Json(new
            {
                Success = true,
                Message = "Success",
                Data = products
            });
        }

        private async Task RenderCatePoduct()
        {
            var cateProduct = await _context.CategoryProducts.Where(cp => cp.ParentId == null).ToListAsync();
            await GetCateChildren(cateProduct);

            var items = new List<CategoryProduct>();
            CreateSelectItem(cateProduct, items, 0);
            var selectList = new SelectList(items, "Id", "CategoryName");

            ViewBag.SelectList = selectList;
        }

        private async Task GetCateChildren(List<CategoryProduct> categories)
        {
            foreach (var category in categories)
            {
                // Lấy tất cả category con của category
                var categoryChildren = await _context.CategoryProducts
                                        .Include(cp => cp.CateProductChildren)
                                        .Where(cp => cp.ParentId == category.Id)
                                        .ToListAsync();

                await GetCateChildren(categoryChildren);

                category.CateProductChildren = categoryChildren;
            }
        }

        private void CreateSelectItem(List<CategoryProduct> sourse, List<CategoryProduct> des, int level)
        {
            string prefix = string.Concat(Enumerable.Repeat("--", level));
            foreach (var cateProduct in sourse)
            {
                des.Add(new CategoryProduct
                {
                    Id = cateProduct.Id,
                    CategoryName = prefix + " " + cateProduct.CategoryName,
                    CreateAt = cateProduct.CreateAt,
                    UpdateAt = cateProduct.UpdateAt,
                });
                if (cateProduct.CateProductChildren.Count > 0)
                {
                    int levelChild = level + 1;
                    CreateSelectItem(cateProduct.CateProductChildren.ToList(), des, levelChild);
                }
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
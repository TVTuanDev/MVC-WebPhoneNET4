using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using WebPhone.EF;
using WebPhone.Models;
using WebPhone.Models.Products;

namespace WebPhone.Repositories
{
    public class ProductRepository : IDisposable
    {
        private readonly AppDbContext _context;

        public ProductRepository()
        {
            _context = new AppDbContext();
        }

        public async Task<CategoryProduct> GetCategoryByIdAsync(Guid id)
        {
            return await _context.CategoryProducts.FindAsync(id);
        }

        public async Task<List<CategoryProduct>> GetListCategoryByParentIdAsync(Guid? id)
        {
            return await _context.CategoryProducts
                    .Where(cp => cp.ParentId == id)
                    .ToListAsync();
        }

        public async Task<StatusResult> CreateCategoryAsync(CateProductDTO cateProductDTO)
        {
            try
            {
                var categoryProduct = new CategoryProduct
                {
                    CategoryName = cateProductDTO.CategoryName,
                    ParentId = cateProductDTO.ParentId
                };

                _context.CategoryProducts.Add(categoryProduct);
                await _context.SaveChangesAsync();

                return new StatusResult
                {
                    Succeeded = true,
                    Message = "Success"
                };
            }
            catch (Exception ex)
            {
                if (ex.InnerException is SqlException sqlEx)
                {
                    if (sqlEx.Number == 2601 || sqlEx.Number == 2627)
                        return new StatusResult
                        {
                            Succeeded = false,
                            Message = "Tên danh mục đã được sử dụng"
                        };
                }
                Console.WriteLine(ex.Message);
                return new StatusResult
                {
                    Succeeded = false,
                    Message = "Lỗi hệ thống"
                };
            }
        }

        public async Task<StatusResult> EditCategoryAsync(CateProductDTO cateProductDTO)
        {
            try
            {
                var categoryProduct = await GetCategoryByIdAsync(cateProductDTO.Id);
                if (categoryProduct == null)
                    return new StatusResult
                    {
                        Succeeded = false,
                        Message = "Không tìm thấy danh mục"
                    };

                _context.CategoryProducts.Attach(categoryProduct);
                categoryProduct.CategoryName = cateProductDTO.CategoryName;
                categoryProduct.ParentId = cateProductDTO.ParentId;
                categoryProduct.UpdateAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return new StatusResult
                {
                    Succeeded = true,
                    Message = "Success"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new StatusResult
                {
                    Succeeded = false,
                    Message = "Lỗi hệ thống"
                };
            }
        }

        public async Task<StatusResult> DeleteCategoryAsync(Guid id)
        {
            try
            {
                var categoryProduct = await _context.CategoryProducts.FindAsync(id);
                if (categoryProduct == null)
                    return new StatusResult
                    {
                        Succeeded = false,
                        Message = "Không tìm thấy danh mục sản phẩm"
                    };

                var cateProductChildren = await _context.CategoryProducts
                                            .Where(cp => cp.ParentId == categoryProduct.Id)
                                            .ToListAsync();
                if (cateProductChildren.Count > 0)
                    return new StatusResult
                    {
                        Succeeded = false,
                        Message = "Không thể xóa danh mục có chứa danh mục con"
                    };

                _context.CategoryProducts.Remove(categoryProduct);
                await _context.SaveChangesAsync();

                return new StatusResult
                {
                    Succeeded = true,
                    Message = "Success"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new StatusResult
                {
                    Succeeded = false,
                    Message = "Lỗi hệ thống"
                };
            }
        }

        public async Task<List<Product>> GetListProductByNameAsync(string q, int page, int itemPerPage = 10)
        {
            return await _context.Products
                            .Where(p => p.ProductName.Contains(q))
                            .OrderBy(p => p.Price)
                            .Skip((page - 1) * itemPerPage)
                            .Take(itemPerPage)
                            .Select(p => p).ToListAsync();
        }

        public async Task<Product> GetProductByIdAsync(Guid id)
        {
            return await _context.Products.FindAsync(id);
        }

        public async Task<StatusResult> CreateAsync(ProductDTO productDTO)
        {
            try
            {
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

                return new StatusResult
                {
                    Succeeded = true,
                    Message = "Success"
                };
            }
            catch (Exception ex)
            {
                if (ex.InnerException is SqlException sqlEx)
                {
                    if (sqlEx.Number == 2601 || sqlEx.Number == 2627)
                        return new StatusResult
                        {
                            Succeeded = false,
                            Message = "Tên danh mục đã được sử dụng"
                        };
                }
                Console.WriteLine(ex.Message);
                return new StatusResult
                {
                    Succeeded = false,
                    Message = "Lỗi hệ thống"
                };
            }
        }

        public async Task<StatusResult> UpdateAsync(ProductDTO productDTO)
        {
            try
            {
                var product = await _context.Products.FindAsync(productDTO.Id);
                if (product == null)
                    return new StatusResult
                    {
                        Succeeded = false,
                        Message = "Không tìm thấy thông tin"
                    };

                product.ProductName = productDTO.ProductName;
                product.Description = productDTO.Description;
                product.Price = productDTO.Price;
                product.Discount = productDTO.Discount;
                product.CategoryId = productDTO.CategoryId;
                product.UpdateAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return new StatusResult
                {
                    Succeeded = true,
                    Message = "Success"
                };
            }
            catch (Exception ex)
            {
                if (ex.InnerException is SqlException sqlEx)
                {
                    if (sqlEx.Number == 2601 || sqlEx.Number == 2627)
                        return new StatusResult
                        {
                            Succeeded = false,
                            Message = "Tên danh mục đã được sử dụng"
                        };
                }
                Console.WriteLine(ex.Message);
                return new StatusResult
                {
                    Succeeded = false,
                    Message = "Lỗi hệ thống"
                };
            }
        }

        public async Task<StatusResult> DeleteAsync(Guid id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                    return new StatusResult
                    {
                        Succeeded = false,
                        Message = "Không tìm thấy sản phẩm"
                    };

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                return new StatusResult
                {
                    Succeeded = true,
                    Message = "Success"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new StatusResult
                {
                    Succeeded = false,
                    Message = "Lỗi hệ thống"
                };
            }
        }

        public async Task GetCateChildren(List<CategoryProduct> categories)
        {
            foreach (var category in categories)
            {
                // Lấy tất cả category con của category
                var categoryChildren = await _context.CategoryProducts
                                        .Where(cp => cp.ParentId == category.Id)
                                        .ToListAsync();

                await GetCateChildren(categoryChildren);

                category.CateProductChildren = categoryChildren;
            }
        }

        public async Task CreateSelectItem(List<CategoryProduct> sourse, List<CategoryProduct> des, int level)
        {
            if (level == 0)
                await GetCateChildren(sourse);

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
                    await CreateSelectItem(cateProduct.CateProductChildren.ToList(), des, levelChild);
                }
            }
        }

        public async Task<SelectList> RenderCatePoduct()
        {
            var cateProduct = await _context.CategoryProducts.Where(cp => cp.ParentId == null).ToListAsync();
            var items = new List<CategoryProduct>();
            await CreateSelectItem(cateProduct, items, 0);

            return new SelectList(items, "Id", "CategoryName");
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
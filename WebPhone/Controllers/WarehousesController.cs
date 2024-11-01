using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using WebPhone.Attributes;
using WebPhone.EF;
using WebPhone.Models.Warehouses;

namespace WebPhone.Controllers
{
    [RoutePrefix("warehouse")]
    [AppAuthorize("Adminitrator, Manage, Employment")]
    public class WarehousesController : Controller
    {
        private readonly AppDbContext _context;

        public WarehousesController()
        {
            _context = new AppDbContext();
        }

        #region CURD Warehouse
        [HttpGet]
        [Route]
        public async Task<ActionResult> Index()
        {
            var warehouses = await _context.Warehouses.ToListAsync();
            return View(warehouses);
        }

        [HttpGet]
        [Route("details")]
        public async Task<ActionResult> Details(Guid? id)
        {
            var warehouse = await _context.Warehouses
                .FirstOrDefaultAsync(m => m.Id == id);
            if (warehouse == null)
            {
                TempData["Message"] = "Error: Không tìm thấy thông tin kho";
                return RedirectToAction(nameof(Index));
            }

            return View(warehouse);
        }

        [HttpGet]
        [Route("create")]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Route("create")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(WarehouseDTO warehouseDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["Message"] = "Error: Vui lòng nhập đầy đủ thông tin";
                    return View(warehouseDTO);
                }

                var warehouse = new Warehouse
                {
                    WarehouseName = warehouseDTO.WarehouseName,
                    Address = warehouseDTO.Address,
                    Capacity = warehouseDTO.Capacity
                };

                _context.Warehouses.Add(warehouse);
                await _context.SaveChangesAsync();

                TempData["Message"] = "Success: Thêm mới thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                TempData["Message"] = "Error: Lỗi hệ thống";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        [Route("edit")]
        public async Task<ActionResult> Edit(Guid? id)
        {
            var warehouse = await _context.Warehouses.FindAsync(id);
            if (warehouse == null)
            {
                TempData["Message"] = "Error: Không tìm thấy thông tin kho";
                return RedirectToAction(nameof(Index));
            }

            var warehouseDTO = new WarehouseDTO
            {
                Id = warehouse.Id,
                WarehouseName = warehouse.WarehouseName,
                Address = warehouse.Address,
                Capacity = warehouse.Capacity
            };

            return View(warehouseDTO);
        }

        [HttpPost]
        [Route("edit")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, WarehouseDTO warehouseDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["Message"] = "Error: Vui lòng nhập đầy đủ thông tin";
                    return View(warehouseDTO);
                }

                if (id != warehouseDTO.Id)
                {
                    TempData["Message"] = "Error: Mã định danh không trùng khớp";
                    return View(warehouseDTO);
                }

                var warehouse = await _context.Warehouses.FindAsync(warehouseDTO.Id);
                if (warehouse == null)
                {
                    TempData["Message"] = "Error: Không tìm thấy thông tin kho";
                    return View(warehouseDTO);
                }

                warehouse.WarehouseName = warehouseDTO.WarehouseName;
                warehouse.Address = warehouseDTO.Address;
                warehouse.Capacity = warehouseDTO.Capacity;
                warehouse.UpdateAt = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["Message"] = "Success: Cập nhật thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                TempData["Message"] = "Error: Lỗi hệ thống";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        [Route("delete")]
        public async Task<ActionResult> Delete(Guid? id)
        {
            var warehouse = await _context.Warehouses
                .FirstOrDefaultAsync(m => m.Id == id);

            if (warehouse == null)
            {
                TempData["Message"] = "Error: Không tìm thấy thông tin kho";
                return RedirectToAction(nameof(Index));
            }

            return View(warehouse);
        }

        [HttpPost, ActionName("Delete")]
        [Route("delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(Guid id)
        {
            var warehouse = await _context.Warehouses.FindAsync(id);
            if (warehouse == null)
            {
                TempData["Message"] = "Error: Không tìm thấy thông tin kho";
                return RedirectToAction(nameof(Index));
            }

            _context.Warehouses.Remove(warehouse);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Success: Xóa kho thành công";
            return RedirectToAction(nameof(Index));
        }
        #endregion

        [HttpGet]
        [Route("import")]
        public async Task<ActionResult> ImportWarehouse()
        {
            ViewData["Warehouses"] = await _context.Warehouses.Take(100).ToListAsync();
            ViewData["Products"] = await _context.Products.Take(100).ToListAsync();
            return View();
        }

        [HttpPost]
        [Route("import")]
        public async Task<JsonResult> ImportWarehouse(InventoryDTO inventoryDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new
                    {
                        Success = false,
                        Message = "Vui lòng nhập đầy đủ thông tin"
                    });

                var warehouse = await _context.Warehouses.FindAsync(inventoryDTO.WarehouseId);
                if (warehouse == null)
                    return Json(new
                    {
                        Success = false,
                        Message = "Không tìm thấy thông tin kho"
                    });

                foreach (var productImport in inventoryDTO.ProductImports)
                {
                    var product = await _context.Products.FindAsync(productImport.ProductId);
                    if (product == null)
                        return Json(new
                        {
                            Success = false,
                            Message = "Không tìm thấy thông tin sản phẩm"
                        });

                    var inventory = new Inventory
                    {
                        ProductId = productImport.ProductId,
                        WarehouseId = warehouse.Id,
                        Quantity = productImport.Quantity,
                        ImportPrice = productImport.ImportPrice,
                    };

                    _context.Inventories.Add(inventory);
                }

                await _context.SaveChangesAsync();

                TempData["Message"] = "Success: Nhập hàng thành công";

                return Json(new
                {
                    Success = true,
                    Message = "Success",
                    Data = Url.Action(nameof(Index))
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new
                {
                    Success = false,
                    Message = "Lỗi hệ thống"
                });
            }
        }

        [HttpPost]
        [Route("filter")]
        public async Task<JsonResult> FilterWarehouseByName(string name)
        {
            name = string.IsNullOrEmpty(name) ? "" : name;

            var warehouses = await _context.Warehouses
                            .Where(w => w.WarehouseName.Contains(name))
                            .Take(100)
                            .Select(w => new Warehouse
                            {
                                Id = w.Id,
                                WarehouseName = w.WarehouseName,
                                Address = w.Address,
                                Capacity = w.Capacity,
                            })
                            .ToListAsync();

            return Json(new
            {
                Success = true,
                Message = "Success",
                Data = warehouses
            });
        }

        [HttpPost]
        [Route("create-warehouse")]
        public async Task<JsonResult> CreateWarehouse(WarehouseDTO warehouseDTO)
        {
            if (!ModelState.IsValid)
                return Json(new
                {
                    Success = false,
                    Message = "Vui lòng nhập đầy đủ thông tin"
                });

            var warehouse = new Warehouse
            {
                Id = Guid.NewGuid(),
                WarehouseName = warehouseDTO.WarehouseName,
                Address = warehouseDTO.Address,
                Capacity = warehouseDTO.Capacity,
            };

            _context.Warehouses.Add(warehouse);
            await _context.SaveChangesAsync();

            warehouseDTO.Id = warehouse.Id;

            return Json(new
            {
                Success = true,
                Message = "Success",
                Data = warehouseDTO
            });
        }
    }
}
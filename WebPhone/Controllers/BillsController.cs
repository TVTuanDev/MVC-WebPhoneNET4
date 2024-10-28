using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using WebPhone.EF;
using WebPhone.Models.Bills;

namespace WebPhone.Controllers
{
    [RoutePrefix("bill")]
    public class BillsController : Controller
    {
        private readonly AppDbContext _context;

        public BillsController()
        {
            _context = new AppDbContext();
        }

        [HttpGet]
        [Route]
        public async Task<ActionResult> Index()
        {
            var appDbContext = _context.Bills.Include(b => b.Customer).Include(b => b.Employment);
            return View(await appDbContext.ToListAsync());
        }

        [HttpGet]
        [Route("create")]
        public async Task<ActionResult> Create()
        {
            ViewData["Customers"] = await _context.Users.Take(100).ToListAsync();
            ViewData["Products"] = await _context.Products.Take(100).ToListAsync();
            return View();
        }

        [HttpPost]
        [Route("create")]
        public async Task<JsonResult> Create(BillDTO billDTO)
        {
            try
            {
                var customer = await _context.Users.FirstOrDefaultAsync(u => u.Id == billDTO.CustomerId);
                if (customer == null)
                    return Json(new
                    {
                        Success = false,
                        Message = "Không tìm thấy thông tin khách hàng"
                    });

                // Lấy thông tin nhân viên
                var employment = await _context.Users.FirstOrDefaultAsync(u => u.Email == "admin@gmail.com");
                if (employment == null)
                    return Json(new
                    {
                        Success = false,
                        Message = "Không tìm thấy thông tin nhân viên"
                    });

                if (billDTO.ProductId.Count != billDTO.Quantities.Count)
                    return Json(new
                    {
                        Success = false,
                        Message = "Dữ liệu số lượng sản phẩm không hợp lệ"
                    });

                var products = new List<BillProductDTO>();

                int price = 0;
                int discount = 0;
                int totalPrice = 0;

                // Tính tổng tiền sản phẩm
                for (int i = 0; i < billDTO.ProductId.Count; i++)
                {
                    var product = await _context.Products.FindAsync(billDTO.ProductId[i]);
                    if (product == null)
                        return Json(new
                        {
                            Success = false,
                            Message = "Không tìm thấy thông tin sản phẩm"
                        });

                    var billProduct = new BillProductDTO
                    {
                        Product = product,
                        Quantity = billDTO.Quantities[i]
                    };

                    products.Add(billProduct);
                    price += (product.Discount ?? product.Price) * billDTO.Quantities[i];
                }

                // Tính tiền giảm giá
                // Nếu kiểu giảm giá là phần trăm
                if (billDTO.DiscountStyle == DiscountStyle.Percent)
                {
                    discount = (int)Math.Ceiling((double)price / 100 * billDTO.DiscountValue / 1000) * 1000;
                }
                // Nếu kiểu giảm giá là số tiền
                if (billDTO.DiscountStyle == DiscountStyle.Money)
                {
                    discount = (int)Math.Ceiling((double)billDTO.DiscountValue / 1000) * 1000;
                }

                discount = discount > price ? price : discount;

                totalPrice = price - discount;

                var bill = new Bill
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    CustomerName = customer.UserName,
                    Price = price,
                    DiscountStyle = billDTO.DiscountStyle,
                    DiscountStyleValue = billDTO.DiscountValue,
                    Discount = discount,
                    TotalPrice = totalPrice,
                    PaymentPrice = billDTO.PaymentValue > totalPrice ? totalPrice : billDTO.PaymentValue,
                    EmploymentId = employment.Id,
                    EmploymentName = employment.UserName
                };

                _context.Bills.Add(bill);

                foreach (var item in products)
                {
                    var billInfo = new BillInfo
                    {
                        ProductId = item.Product.Id,
                        ProductName = item.Product.ProductName,
                        Quantity = item.Quantity,
                        Price = item.Product.Price,
                        Discount = item.Product.Discount,
                        BillId = bill.Id,
                    };

                    _context.BillInfos.Add(billInfo);
                }

                if (billDTO.PaymentValue > 0)
                {
                    var paymentLog = new PaymentLog
                    {
                        Price = billDTO.PaymentValue,
                        CustomerId = customer.Id,
                        BillId = bill.Id
                    };

                    _context.PaymentLogs.Add(paymentLog);
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    Success = true,
                    Message = "Success",
                    Data = Url.Action(nameof(Export), new { id = bill.Id })
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
        
        [HttpGet]
        [Route("export")]
        public async Task<ActionResult> Export(Guid id)
        {
            var bill = await _context.Bills
                        .Include(b => b.Customer)
                        .Include(b => b.BillInfos)
                        .FirstOrDefaultAsync(b => b.Id == id);

            if (bill == null)
            {
                TempData["Message"] = "Error: Không tìm thấy hóa đơn";
                return RedirectToAction(nameof(Create));
            }

            return View(bill);
        }

        [HttpGet]
        [Route("edit")]
        public async Task<ActionResult> Edit(Guid? id)
        {
            var bill = await _context.Bills
                        .Include(b => b.Customer)
                        .Include(b => b.BillInfos)
                        .FirstOrDefaultAsync(b => b.Id == id);

            if (bill == null)
            {
                TempData["Message"] = "Error: Không tìm thấy hóa đơn";
                return RedirectToAction(nameof(Index));
            }

            ViewData["Products"] = await _context.Products.Take(100).ToListAsync();

            return View(bill);
        }

        [HttpPost]
        [Route("edit")]
        public async Task<JsonResult> Edit(Guid id, BillDTO billDTO)
        {
            try
            {
                if (id != billDTO.Id)
                    return Json(new
                    {
                        Success = false,
                        Message = "Dữ liệu hóa đơn không hợp lệ"
                    });

                var bill = await _context.Bills.Include(b => b.BillInfos).FirstOrDefaultAsync(b => b.Id == billDTO.Id);
                if (bill == null)
                    return Json(new
                    {
                        Success = false,
                        Message = "Không tìm thấy thông tin hóa đơn"
                    });

                if (billDTO.ProductId.Count != billDTO.Quantities.Count)
                    return Json(new
                    {
                        Success = false,
                        Message = "Dữ liệu số lượng sản phẩm không hợp lệ"
                    });

                // Lấy id product trong db
                var productIdInDb = bill.BillInfos.Select(bi => bi.ProductId).ToList();
                // lấy id product mới cần sửa
                var productIdNew = billDTO.ProductId;

                int price = 0;
                int discount = 0;
                int totalPrice = 0;

                foreach (var guidId in productIdInDb)
                {
                    // Nếu id product trong db không có trong id mới thì xóa billInfo trong db
                    if (!productIdNew.Contains(guidId))
                    {
                        var billInfo = bill.BillInfos.FirstOrDefault(bi => bi.ProductId == guidId);
                        if (billInfo == null) break;

                        _context.BillInfos.Remove(billInfo);
                    }
                }

                foreach (var guidId in productIdNew)
                {
                    int quantity = billDTO.Quantities[productIdNew.IndexOf(guidId)];
                    var product = await _context.Products.FindAsync(guidId);
                    if (product == null) break;

                    // Nếu id mới không có trong db thì thêm mới
                    if (!productIdInDb.Contains(guidId))
                    {
                        var billInfo = new BillInfo
                        {
                            Id = Guid.NewGuid(),
                            BillId = bill.Id,
                            ProductId = product.Id,
                            ProductName = product.ProductName,
                            Price = product.Price,
                            Discount = product.Discount,
                            Quantity = quantity
                        };

                        _context.BillInfos.Add(billInfo);
                    }

                    price += (product.Discount ?? product.Price) * quantity;
                }

                await _context.SaveChangesAsync();

                foreach (var guidId in productIdNew)
                {
                    int quantity = billDTO.Quantities[productIdNew.IndexOf(guidId)];
                    // Thay đổi quantity với những product cũ
                    var billInfoInBill = bill.BillInfos.FirstOrDefault(bi => bi.ProductId == guidId);

                    if (billInfoInBill == null) break;

                    if (billInfoInBill.Quantity != quantity)
                    {
                        billInfoInBill.Quantity = quantity;
                        billInfoInBill.UpdateAt = DateTime.Now;

                        //_context.BillInfos.Update(billInfoInBill);
                    }
                }

                if (billDTO.DiscountStyle == DiscountStyle.Percent)
                {
                    discount = (int)Math.Ceiling((double)price / 100 * billDTO.DiscountValue / 1000) * 1000;
                }
                if (billDTO.DiscountStyle == DiscountStyle.Money)
                {
                    discount = (int)Math.Ceiling((double)billDTO.DiscountValue / 1000) * 1000;
                }

                discount = discount > price ? price : discount;
                totalPrice = price - discount;

                bill.Price = price;
                bill.DiscountStyle = billDTO.DiscountStyle;
                bill.DiscountStyleValue = billDTO.DiscountValue;
                bill.Discount = discount;
                bill.TotalPrice = totalPrice;
                bill.PaymentPrice = billDTO.PaymentValue;
                bill.UpdateAt = DateTime.Now;

                //_context.Bills.Update(bill);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    Success = true,
                    Message = "Success",
                    Data = Url.Action(nameof(Export), new { id = bill.Id })
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

        [HttpPost, ActionName("Delete")]
        [Route("delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(Guid id)
        {
            try
            {
                var bill = await _context.Bills.FindAsync(id);
                if (bill == null)
                {
                    TempData["Message"] = "Error: Không tìm thấy hóa đơn";
                    return RedirectToAction(nameof(Index));
                }

                _context.Bills.Remove(bill);
                await _context.SaveChangesAsync();

                TempData["Message"] = "Success: Xóa hóa đơn thành công";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                TempData["Message"] = "Error: Lỗi hệ thống";
                return RedirectToAction(nameof(Index));
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
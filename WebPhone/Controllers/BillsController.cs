using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using WebPhone.Attributes;
using WebPhone.EF;
using WebPhone.Models.Bills;
using WebPhone.Repositories;

namespace WebPhone.Controllers
{
    [RoutePrefix("bill")]
    [AppAuthorize("Adminitrator, Manage, Employment")]
    public class BillsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly BillRepository _billRepository;

        public BillsController(BillRepository billRepository)
        {
            _context = new AppDbContext();
            _billRepository = billRepository;
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
                if(!ModelState.IsValid)
                    return Json(new
                    {
                        Success = false,
                        Message = "Vui lòng nhập đầy đủ thông tin"
                    });

                var result = await _billRepository.CreateAsync(billDTO);
                if(!result.Succeeded)
                    return Json(new
                    {
                        Success = false,
                        Message = result.Message
                    });

                var bill = result.Data as Bill;

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

        [HttpPost, ActionName("Delete")]
        [Route("delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(Guid id)
        {
            try
            {
                var result = await _billRepository.DeleteAsync(id);
                if(!result.Succeeded)
                {
                    TempData["Message"] = $"Error: {result.Message}";
                    return RedirectToAction(nameof(Index));
                }

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
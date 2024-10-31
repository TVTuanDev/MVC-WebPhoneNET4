using MimeKit.Tnef;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using WebPhone.Attributes;
using WebPhone.EF;
using WebPhone.Models.Debts;
using WebPhone.Repositories;

namespace WebPhone.Controllers
{
    [RoutePrefix("debt")]
    [AppAuthorize("Adminitrator, Manage, Employment")]
    public class DebtsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly DebtRepository _debtRepository;

        private readonly int ITEM_PER_PAGE = 10;

        public DebtsController(DebtRepository debtRepository)
        {
            _context = new AppDbContext();
            _debtRepository = debtRepository;
        }

        [HttpGet]
        [Route]
        public async Task<ActionResult> Index(string q, int page = 1)
        {
            try
            {
                q = string.IsNullOrEmpty(q) ? "" : q;

                var result = await _debtRepository.GetListCustomerByDebtAsync(q, page, ITEM_PER_PAGE);
                if (!result.Succeeded)
                {
                    TempData["Message"] = $"Error: {result.Message}";
                    return View("Index", "Home");
                }

                var customers = result.Data.Customers as List<CustomerDebtDTO>;

                ViewBag.CountPage = (int)result.Data.CountPage;

                return View(customers);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                TempData["Message"] = "Error: Lỗi hệ thống";
                return View("Index", "Home");
            }
        }

        [HttpGet]
        [Route("customer/details")]
        public async Task<ActionResult> DetailCustomer(Guid id)
        {
            try
            {
                if(id == null)
                {
                    TempData["Message"] = "Error: Dữ liệu không hợp lệ";
                    return RedirectToAction(nameof(Index));
                }

                var result = await _debtRepository.GetListBillByCustomerAsync(id);
                if (!result.Succeeded)
                {
                    TempData["Message"] = $"Error: {result.Message}";
                    return RedirectToAction(nameof(Index));
                }

                var customer = result.Data.Customer as User;
                var customerBills = result.Data.CustomerBills as List<CustomerBillDTO>;

                ViewBag.CustomerBills = customerBills;

                return View(customer);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                TempData["Message"] = "Error: Lỗi hệ thống";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        [Route("bill/details")]
        public async Task<ActionResult> DetailBill(Guid id)
        {
            try
            {
                if (id == null)
                {
                    TempData["Message"] = "Error: Dữ liệu không hợp lệ";
                    return RedirectToAction(nameof(Index));
                }

                var result = await _debtRepository.GetDetailBillAsync(id);
                if (!result.Succeeded)
                {
                    TempData["Message"] = $"Error: {result.Message}";
                    return RedirectToAction(nameof(Index));
                }

                var bill = result.Data.Bill as Bill;
                var paymentLogs = result.Data.PaymentLogs as List<PaymentLog>;

                ViewBag.PaymentLogs = paymentLogs;

                return View(bill);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                TempData["Message"] = "Error: Lỗi hệ thống";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        [Route("payment")]
        public async Task<ActionResult> PaymentDebt(Guid id)
        {
            try
            {
                var result = await _debtRepository.GetListBillIsNotPaymentAllAsync(id);
                if (!result.Succeeded)
                {
                    TempData["Message"] = $"Error: {result.Message}";
                    return RedirectToAction(nameof(Index));
                }

                var paymentDebts = result.Data.PaymentDebts as List<PaymentDebtDTO>;

                ViewData["Customer"] = result.Data.Customer as User;

                return View(paymentDebts);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                TempData["Message"] = "Error: Lỗi hệ thống";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [Route("payment")]
        public async Task<ActionResult> PaymentDebt(Guid id, List<Guid> billIds, int? paymentValue)
        {
            try
            {
                if (paymentValue == null)
                {
                    TempData["Message"] = "Error: Vui lòng nhập đầy đủ thông tin";
                    return RedirectToAction(nameof(PaymentDebt), new { id });
                }

                int payment = paymentValue ?? 0;

                var ressult = await _debtRepository.PaymentBillAsync(id, billIds, payment);
                if (!ressult.Succeeded)
                {
                    TempData["Message"] = $"Error: {ressult.Message}";
                    return RedirectToAction(nameof(PaymentDebt), new { id });
                }

                TempData["Message"] = "Success: Thanh toán thành công";

                return RedirectToAction(nameof(PaymentDebt), new { id });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                TempData["Message"] = "Error: Lỗi hệ thống";
                return RedirectToAction(nameof(PaymentDebt), new { id });
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
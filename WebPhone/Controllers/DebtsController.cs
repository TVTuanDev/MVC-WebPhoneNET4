using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using WebPhone.EF;
using WebPhone.Models.Debts;

namespace WebPhone.Controllers
{
    [RoutePrefix("debt")]
    public class DebtsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly int ITEM_PER_PAGE = 10;

        public DebtsController()
        {
            _context = new AppDbContext();
        }

        [HttpGet]
        [Route]
        public async Task<ActionResult> Index(string q, int page = 1)
        {
            var users = await (from u in _context.Users
                               where !(from ur in _context.UserRoles
                                       select ur.UserId).Contains(u.Id)
                               select u).ToListAsync();

            var userList = new List<User>();
            page = page < 1 ? 1 : page;

            q = string.IsNullOrEmpty(q) ? "" : q;

            int total = users.Where(u => u.Email.Contains(q)).Count();
            int countPage = (int)Math.Ceiling((double)total / ITEM_PER_PAGE);
            countPage = countPage < 1 ? 1 : countPage;
            page = page > countPage ? countPage : page;
            userList = users.Where(u => u.Email.Contains(q))
                        .Skip((page - 1) * ITEM_PER_PAGE)
                        .Take(ITEM_PER_PAGE)
                        .Select(u => new User
                        {
                            Id = u.Id,
                            UserName = u.UserName,
                            Email = u.Email,
                            EmailConfirmed = u.EmailConfirmed,
                            CreateAt = u.CreateAt,
                            UpdateAt = u.UpdateAt,
                        }).ToList();

            countPage = countPage < 1 ? 1 : countPage;
            ViewBag.CountPage = countPage;

            var customers = new List<CustomerDebtDTO>();

            foreach (var user in userList)
            {
                var billByUser = await _context.Bills.Where(b => b.CustomerId == user.Id).ToListAsync();
                var totalPrice = billByUser.Sum(b => b.TotalPrice);
                var payment = await _context.PaymentLogs.Where(p => p.CustomerId == user.Id).SumAsync(p => (int?)p.Price) ?? 0;
                var debt = totalPrice - payment;
                debt = debt < 0 ? 0 : debt;

                var customerDebt = new CustomerDebtDTO
                {
                    Customer = user,
                    Debt = debt,
                };

                customers.Add(customerDebt);
            }

            return View(customers);
        }

        [HttpGet]
        [Route("customer/details")]
        public async Task<ActionResult> DetailCustomer(Guid id)
        {
            var customer = await _context.Users
                            .Include(u => u.CustomerBills)
                            .Include(u => u.PaymentLogs)
                            .FirstOrDefaultAsync(u => u.Id == id);

            if (customer == null)
            {
                TempData["Message"] = "Error: Không tìm thấy thông tin khách hàng";
                return RedirectToAction(nameof(Index));
            }

            var customerBills = new List<CustomerBillDTO>();

            var groupPayment = customer.PaymentLogs.GroupBy(p => p.BillId).ToList();

            foreach (var payment in groupPayment)
            {
                var paylog = payment.OrderBy(p => p.CreateAt).ToList();
                //if (paylog.Count > 0) paylog.RemoveAt(0);

                foreach (var pay in paylog)
                {
                    var customerBill = new CustomerBillDTO
                    {
                        BillDate = pay.CreateAt,
                        PaymentPrice = pay.Price,
                        IsPayment = true,
                        BillId = pay.BillId
                    };

                    customerBills.Add(customerBill);
                }

            }

            foreach (var bill in customer.CustomerBills)
            {
                var customerBill = new CustomerBillDTO
                {
                    BillDate = bill.CreateAt,
                    TotalPrice = bill.TotalPrice,
                    PaymentPrice = bill.PaymentPrice,
                    BillId = bill.Id
                };

                customerBills.Add(customerBill);
            }

            ViewBag.CustomerBills = customerBills.OrderByDescending(cb => cb.BillDate);

            return View(customer);
        }

        [HttpGet]
        [Route("bill/details")]
        public async Task<ActionResult> DetailBill(Guid id)
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

            var paymentLogs = await _context.PaymentLogs
                                .Where(p => p.BillId == bill.Id)
                                .ToListAsync();

            bill.PaymentPrice = paymentLogs.Sum(p => p.Price);

            ViewBag.PaymentLogs = paymentLogs.OrderByDescending(p => p.CreateAt);

            return View(bill);
        }

        [HttpGet]
        [Route("payment")]
        public async Task<ActionResult> PaymentDebt(Guid id)
        {
            var customer = await _context.Users.FindAsync(id);
            if (customer == null)
            {
                TempData["Message"] = "Error: Không tìm thấy thông tin";
                return RedirectToAction(nameof(Index));
            }

            ViewData["Customer"] = customer;

            // Lấy những hóa đơn chưa được thanh toán hoặc thanh toán không hết
            var bills = await _context.Bills.Where(b => b.CustomerId == customer.Id &&
                            (!_context.PaymentLogs.Any(p => p.BillId == b.Id) ||
                            _context.PaymentLogs.Where(pl => pl.BillId == b.Id)
                            .Sum(pl => pl.Price) < b.TotalPrice))
                            .ToListAsync();

            var paymentDebts = new List<PaymentDebtDTO>();

            foreach (var bill in bills)
            {
                var paymentDebt = new PaymentDebtDTO
                {
                    Bill = bill,
                    PaymentTotal = await _context.PaymentLogs.Where(p => p.BillId == bill.Id).SumAsync(pl => (int?)pl.Price) ?? 0
                };

                paymentDebts.Add(paymentDebt);
            }

            return View(paymentDebts);
        }

        [HttpPost]
        [Route("payment")]
        public async Task<ActionResult> PaymentDebt(Guid id, List<Guid> billIds, int? paymentValue)
        {
            if (paymentValue == null)
            {
                TempData["Message"] = "Error: Vui lòng nhập đầy đủ thông tin";
                return RedirectToAction(nameof(PaymentDebt), new { id });
            }

            var customer = await _context.Users.FindAsync(id);
            if (customer == null)
            {
                TempData["Message"] = "Error: Không tìm thấy thông tin khách hàng";
                return RedirectToAction(nameof(PaymentDebt), new { id });
            }

            var bills = new List<Bill>();
            if (billIds == null)
            {
                bills = await _context.Bills.Where(b => b.CustomerId == customer.Id &&
                            (!_context.PaymentLogs.Any(p => p.BillId == b.Id) ||
                            _context.PaymentLogs.Where(pl => pl.BillId == b.Id)
                            .Sum(pl => pl.Price) < b.TotalPrice))
                            .ToListAsync();
            }
            else
            {
                foreach (Guid guidId in billIds)
                {
                    var bill = await _context.Bills.FindAsync(guidId);
                    if (bill == null)
                    {
                        TempData["Message"] = "Error: Không tìm thấy thông tin hóa đơn";
                        return RedirectToAction(nameof(PaymentDebt), new { id });
                    }

                    bills.Add(bill);
                }
            }

            int price = paymentValue ?? 0;

            var paymentLogs = await _context.PaymentLogs.Where(p => p.CustomerId == customer.Id).ToListAsync();

            foreach (var bill in bills)
            {
                var paymentInBill = paymentLogs.Where(p => p.BillId == bill.Id).Sum(p => p.Price);
                var paymentPrice = bill.TotalPrice - paymentInBill;
                if (price <= 0) break;
                else if (price >= paymentPrice)
                {
                    var paymentLog = new PaymentLog
                    {
                        BillId = bill.Id,
                        CustomerId = customer.Id,
                        Price = paymentPrice,
                    };

                    _context.PaymentLogs.Add(paymentLog);

                    price -= paymentPrice;
                }
                else
                {
                    var paymentLog = new PaymentLog
                    {
                        BillId = bill.Id,
                        CustomerId = customer.Id,
                        Price = price,
                    };

                    _context.PaymentLogs.Add(paymentLog);
                    break;
                }
            }

            await _context.SaveChangesAsync();

            TempData["Message"] = "Success: Thanh toán thành công";

            return RedirectToAction(nameof(PaymentDebt), new { id });
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
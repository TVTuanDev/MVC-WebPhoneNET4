using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using WebPhone.EF;
using WebPhone.Models;
using WebPhone.Models.Debts;

namespace WebPhone.Repositories
{
    public class DebtRepository : IDisposable
    {
        private readonly AppDbContext _context;

        public DebtRepository()
        {
            _context = new AppDbContext();
        }

        public async Task<StatusResult> GetListCustomerByDebtAsync(string q, int page, int itemPerPage)
        {
            try
            {
                var users = await (from u in _context.Users
                                   where !(from ur in _context.UserRoles
                                           select ur.UserId)
                                           .Contains(u.Id)
                                   select u).ToListAsync();

                var userList = new List<User>();
                page = page < 1 ? 1 : page;

                int total = users.Where(u => u.Email.Contains(q)).Count();
                int countPage = (int)Math.Ceiling((double)total / itemPerPage);
                countPage = countPage < 1 ? 1 : countPage;
                page = page > countPage ? countPage : page;
                userList = users.Where(u => u.Email.Contains(q))
                            .Skip((page - 1) * itemPerPage)
                            .Take(itemPerPage)
                            .Select(u => new User
                            {
                                Id = u.Id,
                                UserName = u.UserName,
                                Email = u.Email,
                                EmailConfirmed = u.EmailConfirmed,
                                CreateAt = u.CreateAt,
                                UpdateAt = u.UpdateAt,
                            }).ToList();

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

                return new StatusResult
                {
                    Succeeded = true,
                    Message = "Success",
                    Data = new
                    {
                        Customers = customers,
                        CountPage = countPage
                    }
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

        public async Task<StatusResult> GetListBillByCustomerAsync(Guid customerId)
        {
            try
            {
                var customer = await _context.Users
                    .Include(c => c.PaymentLogs)
                    .Include(c => c.CustomerBills)
                    .FirstOrDefaultAsync(c => c.Id == customerId);

                if (customer == null)
                    return new StatusResult
                    {
                        Succeeded = false,
                        Message = "Không tìm thấy thông tin"
                    };

                var customerBills = new List<CustomerBillDTO>();

                var groupPayment = customer.PaymentLogs.GroupBy(p => p.BillId).ToList();

                foreach (var payment in groupPayment)
                {
                    var paylog = payment.OrderBy(p => p.CreateAt).ToList();

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

                customerBills = customerBills.OrderByDescending(cb => cb.BillDate).ToList();

                return new StatusResult
                {
                    Succeeded = true,
                    Message = "Success",
                    Data = new
                    {
                        Customer = customer,
                        CustomerBills = customerBills
                    }
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

        public async Task<StatusResult> GetDetailBillAsync(Guid billId)
        {
            try
            {
                var bill = await _context.Bills
                        .Include(b => b.Customer)
                        .Include(b => b.BillInfos)
                        .FirstOrDefaultAsync(b => b.Id == billId);

                if (bill == null)
                    return new StatusResult
                    {
                        Succeeded = false,
                        Message = "Không tìm thấy hóa đơn"
                    };

                var paymentLogs = await _context.PaymentLogs
                                    .Where(p => p.BillId == bill.Id)
                                    .ToListAsync();

                bill.PaymentPrice = paymentLogs.Sum(p => p.Price);

                paymentLogs = paymentLogs.OrderByDescending(p => p.CreateAt).ToList();

                return new StatusResult
                {
                    Succeeded = true,
                    Message = "Success",
                    Data = new
                    {
                        Bill = bill,
                        PaymentLogs = paymentLogs
                    }
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

        public async Task<StatusResult> GetListBillIsNotPaymentAllAsync(Guid customerId)
        {
            try
            {
                var customer = await _context.Users.FindAsync(customerId);
                if (customer == null)
                    return new StatusResult
                    {
                        Succeeded = false,
                        Message = "Không tìm thấy thông tin"
                    };

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

                return new StatusResult
                {
                    Succeeded = true,
                    Message = "Success",
                    Data = new
                    {
                        Customer = customer,
                        PaymentDebts = paymentDebts
                    }
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

        public async Task<StatusResult> PaymentBillAsync(Guid customerId, List<Guid> billIds, int paymentValue)
        {
            try
            {
                var customer = await _context.Users.FindAsync(customerId);
                if (customer == null)
                    return new StatusResult
                    {
                        Succeeded = false,
                        Message = "Không tìm thấy thông tin khách hàng"
                    };

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
                        var bill = await _context.Bills
                                    .FirstOrDefaultAsync(b => b.CustomerId == customer.Id
                                    && b.Id == guidId);

                        if (bill == null)
                            return new StatusResult
                            {
                                Succeeded = false,
                                Message = "Không tìm thấy thông tin hóa đơn"
                            };

                        bills.Add(bill);
                    }
                }

                int price = paymentValue;

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

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
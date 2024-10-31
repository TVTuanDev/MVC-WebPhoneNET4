using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using WebPhone.EF;
using WebPhone.Models;
using WebPhone.Models.Bills;

namespace WebPhone.Repositories
{
    public class BillRepository : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly UserRepository _userRepository;

        public BillRepository(UserRepository userRepository)
        {
            _context = new AppDbContext();
            _userRepository = userRepository;
        }

        public async Task<StatusResult> CreateAsync(BillDTO billDTO)
        {
            try
            {
                var customer = await _userRepository.GetUserByIdAsync(billDTO.CustomerId);
                if (customer == null)
                    return new StatusResult
                    {
                        Succeeded = false,
                        Message = "Không tìm thấy thông tin khách hàng"
                    };

                // Lấy thông tin nhân viên
                var employment = await _userRepository.GetUserLoginAsync();
                if (employment == null)
                    return new StatusResult
                    {
                        Succeeded = false,
                        Message = "Không tìm thấy thông tin nhân viên"
                    };

                if (billDTO.ProductId.Count != billDTO.Quantities.Count)
                    return new StatusResult
                    {
                        Succeeded = false,
                        Message = "Dữ liệu số lượng sản phẩm không hợp lệ"
                    };

                var products = new List<BillProductDTO>();

                int price = 0;
                int discount = 0;
                int totalPrice = 0;

                // Tính tổng tiền sản phẩm
                for (int i = 0; i < billDTO.ProductId.Count; i++)
                {
                    var product = await _context.Products.FindAsync(billDTO.ProductId[i]);
                    if (product == null)
                        return new StatusResult
                        {
                            Succeeded = false,
                            Message = "Không tìm thấy thông tin sản phẩm"
                        };

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

                return new StatusResult
                {
                    Succeeded = true,
                    Message = "Success",
                    Data = bill
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

        public async Task<StatusResult> DeleteAsync(Guid id)
        {
            try
            {
                var bill = await _context.Bills.FindAsync(id);
                if (bill == null)
                    return new StatusResult
                    {
                        Succeeded = false,
                        Message = "Không tìm thấy hóa đơn"
                    };

                _context.Bills.Remove(bill);
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
            _context?.Dispose();
        }
    }
}
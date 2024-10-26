using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using WebPhone.EF;
using WebPhone.Models.Users;
using WebPhone.Models;
using Microsoft.Data.SqlClient;

namespace WebPhone.Controllers
{
    [RoutePrefix("admin/user")]
    public class UsersController : Controller
    {
        private readonly AppDbContext _context;
        private readonly int ITEM_PER_PAGE = 10;

        public UsersController()
        {
            _context = new AppDbContext();
        }

        #region CURD User
        [HttpGet]
        [Route]
        public async Task<ActionResult> Index(string q, int page = 1)
        {
            var userList = new List<User>();
            int countPage;
            page = page < 1 ? 1 : page;

            // Có query truyền vào
            if (!string.IsNullOrEmpty(q))
            {
                int total = await _context.Users.Where(u => u.Email.Contains(q)).CountAsync();
                countPage = (int)Math.Ceiling((double)total / ITEM_PER_PAGE);
                countPage = countPage < 1 ? 1 : countPage;
                page = page > countPage ? countPage : page;
                userList = await _context.Users
                            .Where(u => u.Email.Contains(q))
                            .OrderBy(u => u.UserName)
                            .Skip((page - 1) * ITEM_PER_PAGE)
                            .Take(ITEM_PER_PAGE)
                            .Select(u => u).ToListAsync();
            }
            else
            {
                int total = await _context.Users.CountAsync();
                countPage = (int)Math.Ceiling((double)total / ITEM_PER_PAGE);
                countPage = countPage < 1 ? 1 : countPage;
                page = page > countPage ? countPage : page;
                userList = await _context.Users
                            .OrderBy(u => u.UserName)
                            .Skip((page - 1) * ITEM_PER_PAGE)
                            .Take(ITEM_PER_PAGE)
                            .Select(u => u).ToListAsync();
            }

            countPage = countPage < 1 ? 1 : countPage;
            ViewBag.CountPage = countPage;

            var roles = await _context.Roles.Select(r => new { r.Id, r.RoleName }).ToListAsync();
            ViewBag.RoleList = new SelectList(roles, "Id", "RoleName");

            return View(userList);
        }

        [HttpGet]
        [Route("details")]
        public async Task<ActionResult> Details(Guid? id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                TempData["Message"] = "Error: Không tìm thấy người dùng";
                return RedirectToAction(nameof(Index));
            }

            return View(user);
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
        public async Task<ActionResult> Create(UserDTO userDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["Message"] = "Error: Vui lòng nhập đầy đủ thông tin";
                    return View(userDTO);
                }

                if (userDTO.Password == null)
                {
                    TempData["Message"] = "Error: Mật khẩu bắt buộc nhập";
                    return View(userDTO);
                }

                var user = new User
                {
                    UserName = userDTO.UserName,
                    Email = userDTO.Email,
                    EmailConfirmed = true,
                    PhoneNumber = userDTO.PhoneNumber,
                    Address = userDTO.Address,
                    PasswordHash = PasswordManager.HashPassword(userDTO.Password)
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                TempData["Message"] = "Success: Thêm mới tài khoản thành công";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                TempData["Message"] = "Error: Lỗi hệ thống";
                return View(userDTO);
            }
        }

        [HttpGet]
        [Route("edit")]
        public async Task<ActionResult> Edit(Guid? id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                TempData["Message"] = "Error: Không tìm thấy thông tin người dùng";
                return RedirectToAction(nameof(Index));
            }

            var userDTO = new UserDTO
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Address = user.Address ?? string.Empty,
            };

            return View(userDTO);
        }

        [HttpPost]
        [Route("edit")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, UserDTO userDTO)
        {
            try
            {
                if (id != userDTO.Id)
                {
                    TempData["Message"] = "Error: Thông tin không hợp lệ";
                    return View(userDTO);
                }

                if (!ModelState.IsValid)
                {
                    TempData["Message"] = "Error: Vui lòng nhập đầy đủ thông tin";
                    return View(userDTO);
                }

                var user = await _context.Users.FindAsync(userDTO.Id);
                if (user == null)
                {
                    TempData["Message"] = "Error: Không tìm thấy thông tin người dùng";
                    return View(userDTO);
                }

                user.UserName = userDTO.UserName;
                user.EmailConfirmed = userDTO.EmailConfirmed;
                user.PhoneNumber = userDTO.PhoneNumber;
                user.Address = userDTO.Address;
                user.UpdateAt = DateTime.UtcNow;

                // Có sự thay đổi về email
                if (user.Email != userDTO.Email)
                {
                    // Kiểm tra xem email mới đã được đăng ký chưa
                    var userByEmail = await _context.Users.FirstOrDefaultAsync(u => u.Email == userDTO.Email);
                    if (userByEmail != null)
                    {
                        TempData["Message"] = "Error: Email đã được sử dụng";
                        return View(userDTO);
                    }

                    user.Email = userDTO.Email;
                }

                // Admin đổi password mới
                if (userDTO.Password != null)
                {
                    user.PasswordHash = PasswordManager.HashPassword(userDTO.Password);
                }

                await _context.SaveChangesAsync();

                TempData["Message"] = "Success: Cập nhật thông tin thành công";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                TempData["Message"] = "Error: Lỗi hệ thống";
                return View(userDTO);
                throw;
            }
        }

        [HttpGet]
        [Route("delete")]
        public async Task<ActionResult> Delete(Guid? id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(m => m.Id == id);

            if (user == null)
            {
                TempData["Message"] = "Error: Không tìm thấy thông tin người dùng";
                return RedirectToAction(nameof(Index));
            }

            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [Route("delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                TempData["Message"] = "Error: Không tìm thấy thông tin người dùng";
                return RedirectToAction(nameof(Index));
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Success: Xóa tài khoản thành công";
            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region Random User
        [HttpGet]
        [Route("random/{count}")]
        public async Task<ActionResult> RamdomUser(int count)
        {
            try
            {
                Random random = new Random();

                string[] address = { "Hà Nội", "Hồ Chí Minh", "Thanh Hóa", "Lạng Sơn", "Quảng Ninh", "Bắc Ninh", "Cần Thơ" };

                var countUser = await _context.Users.CountAsync();

                for (int i = 1; i <= count; i++)
                {
                    var user = new User
                    {
                        UserName = $"Account {++countUser}",
                        Email = $"emailclone{countUser}@gmail.com",
                        PhoneNumber = $"0{RandomGenerator.RandomNumber(9)}",
                        Address = address[random.Next(address.Length)],
                        PasswordHash = PasswordManager.HashPassword("123456"),
                    };

                    _context.Users.Add(user);
                }
                await _context.SaveChangesAsync();

                TempData["Message"] = "Success: Thêm tài khoản thành công";

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
                        TempData["Message"] = "Error: Có email bị trùng";
                        return RedirectToAction(nameof(Index));
                    }
                }

                Console.WriteLine(ex.Message);
                TempData["Message"] = "Error: Lỗi hệ thống";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        [Route("random/delete/{count}")]
        public async Task<ActionResult> DeleteUser(int count)
        {
            try
            {
                var users = await _context.Users
                            .Where(u => u.Email.Contains("emailclone"))
                            .OrderByDescending(u => u.Email)
                            .Take(count)
                            .ToListAsync();

                _context.Users.RemoveRange(users);

                await _context.SaveChangesAsync();

                TempData["Message"] = "Success: Xóa tài khoản thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                TempData["Message"] = "Error: Lỗi hệ thống";
                return RedirectToAction(nameof(Index));
            }
        }
        #endregion

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
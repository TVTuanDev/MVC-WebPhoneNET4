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
using System.Text.RegularExpressions;
using WebPhone.Attributes;
using WebPhone.Repositories;

namespace WebPhone.Controllers
{
    [RoutePrefix("admin/user")]
    [AppAuthorize("Adminitrator, Manage")]
    public class UsersController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserRepository _userRepository;

        private readonly int ITEM_PER_PAGE = 10;

        public UsersController(UserRepository userRepository)
        {
            _context = new AppDbContext();
            _userRepository = userRepository;
        }

        #region CURD User
        [HttpGet]
        [Route]
        public async Task<ActionResult> Index(string q, int page = 1)
        {
            try
            {
                q = q == null ? "" : q;

                int total = await _context.Users.Where(u => u.Email.Contains(q)).CountAsync();
                int countPage = (int)Math.Ceiling((double)total / ITEM_PER_PAGE);
                countPage = countPage < 1 ? 1 : countPage;
                page = page > countPage ? countPage : page;
                page = page < 1 ? 1 : page;

                var userList = await _userRepository.GetListUserByEmailAsync(q, page, ITEM_PER_PAGE);

                ViewBag.CountPage = countPage;

                var roles = await _context.Roles.Select(r => new { r.Id, r.RoleName }).ToListAsync();
                ViewBag.RoleList = new SelectList(roles, "Id", "RoleName");

                return View(userList);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                TempData["Message"] = "Lỗi hệ thống";
                return View("Index", "Home");
            }
        }

        [HttpGet]
        [Route("details")]
        public async Task<ActionResult> Details(Guid id)
        {
            var user = await _userRepository.GetUserByIdAsync(id);

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

                await _userRepository.CreateAsync(user);

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
        public async Task<ActionResult> Edit(Guid id)
        {
            var user = await _userRepository.GetUserByIdAsync(id);
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
            if (!ModelState.IsValid)
            {
                TempData["Message"] = "Error: Vui lòng nhập đầy đủ thông tin";
                return View(userDTO);
            }

            var result = await _userRepository.UpdateAsync(id, userDTO);
            if (!result.Succeeded)
            {
                TempData["Message"] = $"Error: {result.Message}";
                return View(userDTO);
            }

            TempData["Message"] = "Success: Cập nhật thông tin thành công";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Route("delete")]
        public async Task<ActionResult> Delete(Guid id)
        {
            var user = await _userRepository.GetUserByIdAsync(id);

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
            var user = await _userRepository.GetUserByIdAsync(id);
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

        [HttpPost]
        [Route("filter-name")]
        public async Task<JsonResult> FilterCusomterByName(string name)
        {
            try
            {
                name = string.IsNullOrEmpty(name) ? "" : name;

                var users = await _context.Users
                                .Where(u => u.UserName.Contains(name))
                                .Take(100)
                                .ToListAsync();

                var listUser = users.Select(u => new User
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    Address = u.Address,
                }).ToList();

                return Json(new
                {
                    Success = true,
                    Message = "Success",
                    Data = listUser
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpPost]
        [Route("create-customer")]
        public async Task<JsonResult> CreateCustomer(CustomerDTO customerDTO)
        {
            if (!ModelState.IsValid)
                return Json(new
                {
                    Success = false,
                    Message = "Vui lòng nhập đầy đủ thông tin"
                });

            if (!_userRepository.CheckRegexMail(customerDTO.Email))
                return Json(new
                {
                    Success = false,
                    Message = "Vui lòng nhập đúng định dạng email"
                });

            if (!_userRepository.CheckRegexPhoneNumber(customerDTO.PhoneNumber))
                return Json(new
                {
                    Success = false,
                    Message = "Số điện thoại không hợp lệ"
                });

            var user = new User
            {
                UserName = customerDTO.CustomerName,
                Email = customerDTO.Email,
                PhoneNumber = customerDTO.PhoneNumber,
                Address = customerDTO.Address,
                PasswordHash = PasswordManager.HashPassword("123456"),
                EmailConfirmed = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            customerDTO.Id = user.Id;

            return Json(new
            {
                Success = true,
                Message = "Success",
                Data = customerDTO
            });
        }

        [HttpGet]
        [Route("authorize")]
        public async Task<ActionResult> UserAuthorization(Guid id)
        {
            var user = await _userRepository.GetUserByIdAsync(id);
            if (user == null)
            {
                TempData["Message"] = "Error: Không tìm thấy thông tin tài khoản";
                return RedirectToAction(nameof(Index));
            }

            var selectRole = await _context.UserRoles
                                .Where(ur => ur.UserId == user.Id)
                                .Select(ur => ur.RoleId)
                                .ToListAsync();

            var userRoleDTO = new UserRoleDTO
            {
                UserId = user.Id,
                SelectedRole = selectRole
            };

            ViewBag.User = user;
            ViewBag.Roles = await _context.Roles.ToListAsync();

            return View(userRoleDTO);
        }

        [HttpPost]
        [Route("authorize")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UserAuthorization(UserRoleDTO userRoleDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["Message"] = "Error: Thông tin không chính xác";
                    return RedirectToAction(nameof(Index));
                }

                var user = await _userRepository.GetUserByIdAsync(userRoleDTO.UserId);
                if (user == null)
                {
                    TempData["Message"] = "Error: Không tìm thấy thông tin tài khoản";
                    return RedirectToAction(nameof(Index));
                }

                var userRole = await _context.UserRoles.ToListAsync();

                var userRoleByUser = userRole.Where(ur => ur.UserId == user.Id)
                                    .Select(ur => ur.RoleId).ToList();

                foreach (Guid guidId in userRoleByUser)
                {
                    // Nếu user role mới không chứa user role cũ
                    // Xóa user role cũ
                    if (!userRoleDTO.SelectedRole.Contains(guidId))
                    {
                        var userRoleRemove = userRole.FirstOrDefault(ur => ur.RoleId == guidId);
                        if (userRoleRemove == null)
                        {
                            TempData["Message"] = "Error: Không tìm thấy thông tin quyền";
                            return RedirectToAction(nameof(UserAuthorization));
                        }
                        _context.UserRoles.Remove(userRoleRemove);
                    }
                }

                foreach (Guid guidId in userRoleDTO.SelectedRole)
                {
                    // Nếu user role mới ko có trong user role cũ
                    // Thêm user role mới
                    if (!userRoleByUser.Contains(guidId))
                    {
                        var userRoleNew = new UserRole
                        {
                            UserId = user.Id,
                            RoleId = guidId,
                        };
                        _context.UserRoles.Add(userRoleNew);
                    }
                }

                await _context.SaveChangesAsync();

                TempData["Message"] = "Success: Phân quyền thành công";

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
using Org.BouncyCastle.Crypto.Prng;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;
using System.Web.Mvc;
using WebPhone.EF;
using WebPhone.Models;
using WebPhone.Models.Accounts;
using WebPhone.Services;
using System.Runtime.Caching;
using System.Web.Security;
using System.Security.Claims;

namespace WebPhone.Controllers
{
    public class AccountsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly SendMailService _sendMail;
        private readonly ObjectCache _cache = MemoryCache.Default;

        public AccountsController(SendMailService sendMail)
        {
            _context = new AppDbContext();
            _sendMail = sendMail;
        }

        [HttpGet]
        public ActionResult Register()
        {
            if (CheckLogin())
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterDTO registerDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["Message"] = "Error: Vui lòng nhập đầy đủ thông tin";
                    return View(registerDTO);
                }

                if (!CheckRegexMail(registerDTO.Email))
                {
                    TempData["Message"] = "Error: Không đúng định dạng email";
                    return View(registerDTO);
                }

                var userByEmail = await _context.Users.FirstOrDefaultAsync(c => c.Email == registerDTO.Email);
                if (userByEmail != null)
                {
                    TempData["Message"] = "Error: Email đã được sử dụng";
                    return View(registerDTO);
                }

                var user = new User
                {
                    UserName = registerDTO.UserName,
                    Email = registerDTO.Email,
                    PhoneNumber = registerDTO.PhoneNumber,
                    Address = registerDTO.Address,
                    PasswordHash = PasswordManager.HashPassword(registerDTO.Password)
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Gửi code gửi mail xác thực
                await SendMailConfirmCode(registerDTO.Email);

                TempData["Message"] = "Success: Mã xác thực đã được gửi qua hòm thư";

                return RedirectToAction("ConfirmEmail", new { userId = user.Id });
            }
            catch (Exception)
            {
                TempData["Message"] = "Error: Lỗi hệ thống";
                return View(registerDTO);
            }
        }

        [HttpGet]
        public ActionResult Login(string returnUrl = null)
        {
            if (CheckLogin())
                return RedirectToAction("Index", "Home");

            returnUrl = returnUrl == null ? Url.Content("/") : returnUrl;
            ViewData["ReturnUrl"] = returnUrl;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginDTO loginDTO, string returnUrl = null)
        {
            returnUrl = returnUrl == null ? Url.Content("/") : returnUrl;
            ViewData["ReturnUrl"] = returnUrl;

            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["Message"] = "Error: Vui lòng nhập đầy đủ thông tin";
                    return View(loginDTO);
                }

                if (!CheckRegexMail(loginDTO.Email))
                {
                    TempData["Message"] = "Error: Không đúng định dạng email";
                    return View(loginDTO);
                }

                var user = await _context.Users.FirstOrDefaultAsync(c => c.Email == loginDTO.Email);
                if (user is null)
                {
                    TempData["Message"] = "Error: Thông tin tài khoản không chính xác";
                    return View(loginDTO);
                }

                if (!PasswordManager.VerifyPassword(loginDTO.Password, user.PasswordHash))
                {
                    TempData["Message"] = "Error: Thông tin tài khoản không chính xác";
                    return View(loginDTO);
                }

                if (!user.EmailConfirmed)
                {
                    // Gửi code gửi mail xác thực
                    await SendMailConfirmCode(user.Email);

                    TempData["Message"] = "Success: Email chưa được xác thực, vui lòng xác thực email qua hòm thư";
                    return RedirectToAction("ConfirmEmail", new { userId = user.Id });
                }

                // Add cookie xác thực
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Email, user.Email),
                };

                var listRoleName = await (from r in _context.Roles
                                          join ur in _context.UserRoles on r.Id equals ur.RoleId
                                          where ur.UserId == user.Id
                                          select r.RoleName).ToListAsync();

                foreach (var roleName in listRoleName)
                {
                    claims.Add(new Claim(ClaimTypes.Role, roleName));
                }

                FormsAuthentication.SetAuthCookie(user.Email, loginDTO.RememberMe);

                // Tạo claims identity
                var identity = new ClaimsIdentity(claims, "Custom");
                var principal = new ClaimsPrincipal(identity);

                // Tạo cookie cho claims
                var authTicket = new FormsAuthenticationTicket(
                                    1, user.UserName, 
                                    DateTime.Now, 
                                    DateTime.Now.AddDays(30), 
                                    loginDTO.RememberMe, 
                                    string.Join(",", claims.Select(c => c.Value)));

                var encTicket = FormsAuthentication.Encrypt(authTicket);
                var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encTicket);
                Response.Cookies.Add(authCookie);

                return Redirect(returnUrl);
            }
            catch (Exception)
            {
                TempData["Message"] = "Error: Lỗi hệ thống";
                return View(loginDTO);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            // Đăng xuất và xóa cookie
            FormsAuthentication.SignOut();

            return RedirectToAction(nameof(Login));  // Chuyển hướng đến trang login
        }

        [HttpGet]
        public async Task<ActionResult> ConfirmEmail(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                TempData["Message"] = "Error: Không tìm thấy thông tin người dùng";
                return RedirectToAction(nameof(Register));
            }

            ViewBag.Email = user.Email;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ConfirmEmail(EmailConfirmed emailConfirmed)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["Message"] = "Error: Vui lòng nhập đầy đủ thông tin";
                    return View(emailConfirmed);
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == emailConfirmed.Email);
                if (user == null)
                {
                    TempData["Message"] = "Error: Không thấy thông tin người dùng";
                    return View(emailConfirmed);
                }

                if (!VerifyEmail(emailConfirmed.Email, emailConfirmed.Code))
                {
                    TempData["Message"] = "Error: Mã xác thực không đúng hoặc đã hết hạn";
                    return View(emailConfirmed);
                }

                user.EmailConfirmed = true;
                //_context.Users.Update(user);
                await _context.SaveChangesAsync();

                TempData["Message"] = "Success: Xác thực tài khoản thành công";
                return RedirectToAction(nameof(Login));
            }
            catch (Exception)
            {
                TempData["Message"] = "Error: Lỗi hệ thống";
                return View(emailConfirmed);
            }
        }

        [HttpGet]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(string email)
        {
            ViewData["Email"] = email;

            if (!CheckRegexMail(email))
            {
                TempData["Message"] = "Error: Không đúng định dạng email";
                return View();
            }

            var userByEmail = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (userByEmail == null)
            {
                TempData["Message"] = "Error: Email chưa được đăng ký";
                return View();
            }

            // Gửi code gửi mail forgot password
            await SendForgotPasswordCode(email);

            TempData["Message"] = "Success: Mã xác thực đã được gửi qua hòm thư";

            return RedirectToAction("ConfirmForgotPassword", new { userId = userByEmail.Id });
        }

        [HttpGet]
        public async Task<ActionResult> ConfirmForgotPassword(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                TempData["Message"] = "Error: Không tìm thấy thông tin người dùng";
                return View();
            }

            ViewData["Email"] = user.Email;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ConfirmForgotPassword(ForgotPasswordDTO forgotPasswordDTO)
        {
            if (!ModelState.IsValid)
            {
                TempData["Message"] = "Error: Vui lòng nhập đầy đủ thông tin";
                return View(forgotPasswordDTO);
            }

            if (!VerifyEmail(forgotPasswordDTO.Email, forgotPasswordDTO.Code))
            {
                TempData["Message"] = "Error: Mã xác thực không đúng hoặc đã hết hạn";
                return View(forgotPasswordDTO);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == forgotPasswordDTO.Email);
            if (user == null)
            {
                TempData["Message"] = "Error: Không tìm thấy thông tin người dùng";
                return View(forgotPasswordDTO);
            }

            user.PasswordHash = PasswordManager.HashPassword(forgotPasswordDTO.Password);
            user.UpdateAt = DateTime.UtcNow;

            //_context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Success: Đổi mật khẩu mới thành công";

            return RedirectToAction(nameof(Login));
        }

        private bool CheckLogin() => User.Identity.IsAuthenticated;

        private bool CheckRegexMail(string email)
        {
            string pattern = @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$";
            Regex regex = new Regex(pattern);
            if (regex.IsMatch(email)) return true;
            return false;
        }

        private async Task SendMailConfirmCode(string email)
        {
            var code = RandomGenerator.RandomCode(8);
            var htmlMessage = $@"<h3>Bạn đã đăng ký tài khoản trên WebPhone</h3>
                    <p>Tiếp tục đăng ký với WebPhone bằng cách nhập mã bên dưới:</p>
                    <h1>{code}</h1>
                    <p>Mã xác minh sẽ hết hạn sau 10 phút.</p>
                    <p><b>Nếu bạn không yêu cầu mã,</b> bạn có thể bỏ qua tin nhắn này.</p>";

            await _sendMail.SendMailAsync(email, "Xác thực tài khoản", htmlMessage);

            var emailConfirm = new EmailConfirmed
            {
                Email = email,
                Code = code,
            };

            //var policy = new CacheItemPolicy
            //{
            //    AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(10), // Thời gian sống tuyệt đối
            //    SlidingExpiration = TimeSpan.FromMinutes(5) // Thời gian sống trượt
            //};

            _cache.Set(email, emailConfirm, DateTimeOffset.Now.AddMinutes(10));
        }

        private async Task SendForgotPasswordCode(string email)
        {
            var code = RandomGenerator.RandomCode(8);
            var htmlMessage = $@"<h3>Bạn quên mật khẩu tài khoản WebPhone</h3>
                    <p>Lấy lại mật khẩu WebPhone bằng cách nhập mã bên dưới:</p>
                    <h1>{code}</h1>
                    <p>Mã xác minh sẽ hết hạn sau 10 phút.</p>
                    <p><b>Nếu bạn không yêu cầu mã,</b> bạn có thể bỏ qua tin nhắn này.</p>";

            await _sendMail.SendMailAsync(email, "Quên mật khẩu", htmlMessage);

            var emailConfirm = new EmailConfirmed
            {
                Email = email,
                Code = code,
            };

            _cache.Set(email, emailConfirm, DateTimeOffset.Now.AddMinutes(10));
        }

        private bool VerifyEmail(string email, string code)
        {
            var emailConfirm = _cache.Get(email) as EmailConfirmed;

            if (emailConfirm == null) return false;

            if (emailConfirm.Email != email) return false;

            if (emailConfirm.Code != code) return false;

            _cache.Remove(email);

            return true;
        }

        // Dispose để giải phóng tài nguyên
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
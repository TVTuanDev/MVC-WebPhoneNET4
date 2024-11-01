using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using WebPhone.Models.Accounts;
using System.Web.Security;
using WebPhone.Attributes;
using WebPhone.Repositories;

namespace WebPhone.Controllers
{
    [RoutePrefix("customer")]
    public class AccountsController : Controller
    {
        private readonly UserRepository _userRepository;

        public AccountsController(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpGet]
        [Route("register")]
        public ActionResult Register()
        {
            if (_userRepository.CheckLogin())
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        [Route("register")]
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

                var result = await _userRepository.RegisterAsync(registerDTO);
                if(!result.Succeeded)
                {
                    TempData["Message"] = $"Error: {result.Message}";
                    return View(registerDTO);
                }

                // Gửi code gửi mail xác thực
                await _userRepository.SendMailConfirmCodeAsync(registerDTO.Email);

                TempData["Message"] = "Success: Mã xác thực đã được gửi qua hòm thư";

                return RedirectToAction(nameof(ConfirmEmail), new { email = registerDTO.Email });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                TempData["Message"] = "Error: Lỗi hệ thống";
                return View(registerDTO);
            }
        }

        [HttpGet]
        [Route("login")]
        public ActionResult Login(string returnUrl = null)
        {
            if (_userRepository.CheckLogin())
                return RedirectToAction("Index", "Home");

            returnUrl = returnUrl == null ? Url.Content("/") : returnUrl;
            ViewData["ReturnUrl"] = returnUrl;

            return View();
        }

        [HttpPost]
        [Route("login")]
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

                var result = await _userRepository.LoginAsync(loginDTO);
                if (!result.Succeeded)
                {
                    if(result.Code == "EmailIsNotConfirm")
                    {
                        await _userRepository.SendMailConfirmCodeAsync(loginDTO.Email);

                        TempData["Message"] = "Warning: Email chưa được xác thực, vui lòng xác thực email qua hòm thư";
                        return RedirectToAction(nameof(ConfirmEmail), new { email = loginDTO.Email });
                    }

                    TempData["Message"] = $"Error: {result.Message}";
                    return View(loginDTO);
                }

                TempData["Message"] = "Success: Chào mừng quay trở lại";

                return Redirect(returnUrl);
            }
            catch (Exception)
            {
                TempData["Message"] = "Error: Lỗi hệ thống";
                return View(loginDTO);
            }
        }

        [HttpPost]
        [Route("logout")]
        [ValidateAntiForgeryToken]
        [AppAuthorize]
        public ActionResult Logout()
        {
            // Xóa dữ liệu trong session
            Session.Clear();

            // Kết thúc session
            Session.Abandon();

            // Đăng xuất người dùng
            FormsAuthentication.SignOut();

            return RedirectToAction(nameof(Login));  // Chuyển hướng đến trang login
        }

        [HttpGet]
        [Route("confirm-email")]
        public async Task<ActionResult> ConfirmEmail(string email)
        {
            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null)
            {
                TempData["Message"] = "Error: Không tìm thấy thông tin người dùng";
                return RedirectToAction(nameof(Register));
            }

            var emailConfirm = new EmailConfirmed
            {
                Email = user.Email
            };

            return View(emailConfirm);
        }

        [HttpPost]
        [Route("confirm-email")]
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

                var user = await _userRepository.GetUserByEmailAsync(emailConfirmed.Email);
                if (user == null)
                {
                    TempData["Message"] = "Error: Không thấy thông tin người dùng";
                    return View(emailConfirmed);
                }

                if (!_userRepository.VerifyEmail(user.Email, emailConfirmed.Code))
                {
                    TempData["Message"] = "Error: Mã xác thực không đúng hoặc đã hết hạn";
                    return View(emailConfirmed);
                }

                await _userRepository.ConfirmEmailAsync(user);

                TempData["Message"] = "Success: Xác thực tài khoản thành công";
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                TempData["Message"] = "Error: Lỗi hệ thống";
                return View(emailConfirmed);
            }
        }

        [HttpGet]
        [Route("forgot-password")]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [Route("forgot-password")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(string email)
        {
            ViewData["Email"] = email;

            try
            {
                if (!_userRepository.CheckRegexMail(email))
                {
                    TempData["Message"] = "Error: Không đúng định dạng email";
                    return View();
                }

                var userByEmail = await _userRepository.GetUserByEmailAsync(email);
                if (userByEmail == null)
                {
                    TempData["Message"] = "Error: Email chưa được đăng ký";
                    return View();
                }

                // Gửi code gửi mail forgot password
                await _userRepository.SendForgotPasswordCodeAsync(email);

                TempData["Message"] = "Success: Mã xác thực đã được gửi qua hòm thư";

                return RedirectToAction("ConfirmForgotPassword", new { userId = userByEmail.Id });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                TempData["Message"] = "Error: Lỗi hệ thống";
                return View();
            }
        }

        [HttpGet]
        [Route("confirm-forgot-password")]
        public async Task<ActionResult> ConfirmForgotPassword(Guid userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                TempData["Message"] = "Error: Không tìm thấy thông tin người dùng";
                return View();
            }

            var forgotPasswordDTO = new ForgotPasswordDTO
            {
                Email = user.Email,
            };

            return View(forgotPasswordDTO);
        }
        
        [HttpPost]
        [Route("confirm-forgot-password")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ConfirmForgotPassword(ForgotPasswordDTO forgotPasswordDTO)
        {
            if (!ModelState.IsValid)
            {
                TempData["Message"] = "Error: Vui lòng nhập đầy đủ thông tin";
                return View(forgotPasswordDTO);
            }

            if (!_userRepository.VerifyEmail(forgotPasswordDTO.Email, forgotPasswordDTO.Code))
            {
                TempData["Message"] = "Error: Mã xác thực không đúng hoặc đã hết hạn";
                return View(forgotPasswordDTO);
            }

            var user = await _userRepository.GetUserByEmailAsync(forgotPasswordDTO.Email);
            if (user == null)
            {
                TempData["Message"] = "Error: Không tìm thấy thông tin người dùng";
                return View(forgotPasswordDTO);
            }

            await _userRepository.ResetPasswordAsync(user, forgotPasswordDTO.Password);

            TempData["Message"] = "Success: Đặt lại mật khẩu mới thành công";

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        [Route("info")]
        [AppAuthorize]
        public async Task<ActionResult> InfoCustomer()
        {
            var user = await _userRepository.GetUserLoginAsync();
            if (user == null)
            {
                TempData["Message"] = "Error: Không tìm thấy thông tin";
                return RedirectToAction("Index", "Home");
            }

            return View(user);
        }

        [HttpGet]
        [Route("change-password")]
        [AppAuthorize]
        public async Task<ActionResult> ChangePassword()
        {
            var user = await _userRepository.GetUserLoginAsync();
            if (user == null)
            {
                TempData["Message"] = "Error: Không tìm thấy thông tin";
                return RedirectToAction("Index", "Home");
            }

            var changePass = new ChangePasswordDTO { Id = user.Id };

            return View(changePass);
        }

        [HttpPost]
        [Route("change-password")]
        [ValidateAntiForgeryToken]
        [AppAuthorize]
        public async Task<ActionResult> ChangePassword(ChangePasswordDTO changePasswordDTO)
        {
            try
            {
                var user = await _userRepository.GetUserByIdAsync(changePasswordDTO.Id);
                if (user == null)
                {
                    TempData["Message"] = "Error: Không tìm thấy người dùng";
                    return RedirectToAction(nameof(InfoCustomer));
                }

                if (!_userRepository.CheckPassword(user, changePasswordDTO.OldPassword))
                {
                    TempData["Message"] = "Error: Mật khẩu cũ không chính xác";
                    return RedirectToAction(nameof(InfoCustomer));
                }

                await _userRepository.ResetPasswordAsync(user, changePasswordDTO.NewPassword);

                TempData["Message"] = "Success: Cập nhật mật khẩu thành công";

                return RedirectToAction(nameof(InfoCustomer));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                TempData["Message"] = "Error: Lỗi hệ thống";
                return RedirectToAction(nameof(InfoCustomer));
            }
        }
    }
}
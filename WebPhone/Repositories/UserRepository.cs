
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Runtime.Caching;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;
using System.Web.Security;
using WebPhone.EF;
using WebPhone.Models;
using WebPhone.Models.Accounts;
using WebPhone.Models.Users;
using WebPhone.Services;

namespace WebPhone.Repositories
{
    public class UserRepository : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly SendMailService _sendMailService;
        private readonly ObjectCache _cache = MemoryCache.Default;

        public UserRepository(SendMailService sendMailService)
        {
            _context = new AppDbContext();
            _sendMailService = sendMailService;
        }

        public async Task<User> GetUserLoginAsync()
        {
            var claimPrincipal = HttpContext.Current.Session["UserClaim"] as ClaimsPrincipal;
            var email = claimPrincipal.FindFirst(ClaimTypes.Email).Value;
            return await GetUserByEmailAsync(email);
        }

        public async Task<User> GetUserByIdAsync(Guid id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public bool CheckPassword(User user, string password)
        {
            return PasswordManager.VerifyPassword(password, user.PasswordHash);
        }

        public bool CheckRegexMail(string email)
        {
            string pattern = @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$";
            Regex regex = new Regex(pattern);
            if (regex.IsMatch(email)) return true;
            return false;
        }

        public bool CheckRegexPhoneNumber(string phoneNumber)
        {
            string pattern = @"^0[3|5|7|8|9][0-9]{8}$";
            Regex regex = new Regex(pattern);
            if (regex.IsMatch(phoneNumber)) return true;
            return false;
        }

        public bool CheckLogin() => HttpContext.Current.User.Identity.IsAuthenticated;

        public async Task<StatusResult> RegisterAsync(RegisterDTO registerDTO)
        {
            try
            {
                if (!CheckRegexMail(registerDTO.Email))
                    return new StatusResult
                    {
                        Succeeded = false,
                        Message = "Không đúng định dạng email",
                    };

                var userByEmail = await GetUserByEmailAsync(registerDTO.Email);
                if (userByEmail != null)
                    return new StatusResult
                    {
                        Succeeded = false,
                        Message = "Email đã được sử dụng",
                    };

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

                return new StatusResult
                {
                    Succeeded = true,
                    Message = "Success",
                };
            }
            catch (Exception ex)
            {
                return new StatusResult
                {
                    Succeeded = false,
                    Message = ex.Message,
                };
            }
        }

        public async Task<StatusResult> LoginAsync(LoginDTO loginDTO)
        {
            try
            {
                if (!CheckRegexMail(loginDTO.Email))
                    return new StatusResult
                    {
                        Succeeded = false,
                        Message = "Không đúng định dạng email"
                    };

                var user = await GetUserByEmailAsync(loginDTO.Email);
                if (user == null)
                    return new StatusResult
                    {
                        Succeeded = false,
                        Message = "Tài khoản chưa được đăng ký"
                    };

                if (!CheckPassword(user, loginDTO.Password))
                    return new StatusResult
                    {
                        Succeeded = false,
                        Message = "Thông tin tài khoản không chính xác"
                    };

                if (!user.EmailConfirmed)
                    return new StatusResult
                    {
                        Succeeded = false,
                        Code = "EmailIsNotConfirm"
                    };

                // Tạo danh sách các Claim
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Email, user.Email),
                };

                var listRoleName = new List<string>();

                // Lấy danh sách vai trò của người dùng
                listRoleName = await (from r in _context.Roles
                                      join ur in _context.UserRoles on r.Id equals ur.RoleId
                                      where ur.UserId == user.Id
                                      select r.RoleName).ToListAsync();

                // Thêm các Claim vai trò
                foreach (var roleName in listRoleName)
                {
                    claims.Add(new Claim(ClaimTypes.Role, roleName));
                }

                var claimPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, "WebPhone"));

                HttpContext.Current.Session["UserClaim"] = claimPrincipal;
                FormsAuthentication.SetAuthCookie(user.UserName, loginDTO.RememberMe);

                return new StatusResult
                {
                    Succeeded = true,
                    Message = "Success"
                };
            }
            catch (Exception ex)
            {
                return new StatusResult
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
        }

        public async Task SendMailConfirmCodeAsync(string email)
        {
            var code = RandomGenerator.RandomCode(8);
            var htmlMessage = $@"<h3>Bạn đã đăng ký tài khoản trên WebPhone</h3>
                    <p>Tiếp tục đăng ký với WebPhone bằng cách nhập mã bên dưới:</p>
                    <h1>{code}</h1>
                    <p>Mã xác minh sẽ hết hạn sau 10 phút.</p>
                    <p><b>Nếu bạn không yêu cầu mã,</b> bạn có thể bỏ qua tin nhắn này.</p>";

            await _sendMailService.SendMailAsync(email, "Xác thực tài khoản", htmlMessage);

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

        public async Task SendForgotPasswordCodeAsync(string email)
        {
            var code = RandomGenerator.RandomCode(8);
            var htmlMessage = $@"<h3>Bạn quên mật khẩu tài khoản WebPhone</h3>
                    <p>Lấy lại mật khẩu WebPhone bằng cách nhập mã bên dưới:</p>
                    <h1>{code}</h1>
                    <p>Mã xác minh sẽ hết hạn sau 10 phút.</p>
                    <p><b>Nếu bạn không yêu cầu mã,</b> bạn có thể bỏ qua tin nhắn này.</p>";

            await _sendMailService.SendMailAsync(email, "Quên mật khẩu", htmlMessage);

            var emailConfirm = new EmailConfirmed
            {
                Email = email,
                Code = code,
            };

            _cache.Set(email, emailConfirm, DateTimeOffset.Now.AddMinutes(10));
        }

        public bool VerifyEmail(string email, string code)
        {
            var emailConfirm = _cache.Get(email) as EmailConfirmed;

            if (emailConfirm == null) return false;

            if (emailConfirm.Email != email) return false;

            if (emailConfirm.Code != code) return false;

            _cache.Remove(email);

            return true;
        }

        public async Task CreateAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task ConfirmEmailAsync(User user)
        {
            // Theo dõi user
            _context.Users.Attach(user);
            user.EmailConfirmed = true;

            await _context.SaveChangesAsync();
        }

        public async Task ResetPasswordAsync(User user, string newPassword)
        {
            // Theo dõi user
            _context.Users.Attach(user);
            user.PasswordHash = PasswordManager.HashPassword(newPassword);
            user.UpdateAt = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task<List<User>> GetListUserByEmailAsync(string q, int page, int itemPerPage = 10)
        {
            return await _context.Users
                            .Where(u => u.Email.Contains(q))
                            .OrderBy(u => u.UserName)
                            .Skip((page - 1) * itemPerPage)
                            .Take(itemPerPage)
                            .Select(u => u).ToListAsync();
        }

        public async Task<StatusResult> UpdateAsync(Guid id, UserDTO userDTO)
        {
            try
            {
                if (id != userDTO.Id)
                    return new StatusResult
                    {
                        Succeeded = false,
                        Message = "Thông tin không hợp lệ",
                    };

                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return new StatusResult
                    {
                        Succeeded = false,
                        Message = "Không tìm thấy thông tin người dùng",
                    };

                user.UserName = userDTO.UserName;
                user.EmailConfirmed = userDTO.EmailConfirmed;
                user.PhoneNumber = userDTO.PhoneNumber;
                user.Address = userDTO.Address;
                user.UpdateAt = DateTime.Now;

                // Có sự thay đổi về email
                if (user.Email != userDTO.Email)
                {
                    // Kiểm tra xem email mới đã được đăng ký chưa
                    var userByEmail = await GetUserByEmailAsync(userDTO.Email);
                    if (userByEmail != null)
                        return new StatusResult
                        {
                            Succeeded = false,
                            Message = "Email đã được sử dụng",
                        };

                    user.Email = userDTO.Email;
                }

                // Admin đổi password mới
                if (userDTO.Password != null)
                {
                    user.PasswordHash = PasswordManager.HashPassword(userDTO.Password);
                }

                await _context.SaveChangesAsync();

                return new StatusResult
                {
                    Succeeded = true,
                    Message = "Success",
                };
            }
            catch (Exception ex)
            {
                return new StatusResult
                {
                    Succeeded = false,
                    Message = ex.Message,
                };
            }
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
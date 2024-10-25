using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebPhone.Models.Accounts
{
    public class ForgotPasswordDTO
    {
        [Display(Name = "Email quên mật khẩu")]
        [Required(ErrorMessage = "{0} bắt buộc nhập")]
        [EmailAddress(ErrorMessage = "Vui lòng nhập đúng định dạng email")]
        public string Email { get; set; }

        [Display(Name = "Mã xác thực")]
        [Required(ErrorMessage = "{0} bắt buộc nhập")]
        public string Code { get; set; }

        [Display(Name = "Mật khẩu mới")]
        [Required(ErrorMessage = "{0} bắt buộc nhập")]
        [DataType(DataType.Password)]
        [StringLength(20, MinimumLength = 6, ErrorMessage = "{0} phải dài từ {2} đến {1} ký tự")]
        public string Password { get; set; }

        [Display(Name = "Nhập lại khẩu mới")]
        [Required(ErrorMessage = "{0} bắt buộc nhập")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Nhập lại mật khẩu không đúng")]
        public string ConfirmPassword { get; set; }
    }
}
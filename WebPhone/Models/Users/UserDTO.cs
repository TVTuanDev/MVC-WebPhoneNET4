using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebPhone.Models.Users
{
    public class UserDTO
    {
        public Guid Id { get; set; }

        [Display(Name = "Họ tên")]
        [Required(ErrorMessage = "{0} bắt buộc nhập")]
        public string UserName { get; set; }

        [Display(Name = "Email")]
        [Required(ErrorMessage = "{0} bắt buộc nhập")]
        [RegularExpression(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$", ErrorMessage = "Vui lòng nhập đúng định dạng email")]
        public string Email { get; set; }

        [Display(Name = "Số điện thoại")]
        [Required(ErrorMessage = "{0} bắt buộc nhập")]
        [RegularExpression(@"^0[3|5|7|8|9][0-9]{8}$", ErrorMessage = "Vui lòng nhập đúng định dạng {0}")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Địa chỉ")]
        [Required(ErrorMessage = "{0} bắt buộc nhập")]
        public string Address { get; set; }

        [Display(Name = "Mật khẩu")]
        //[Required(ErrorMessage = "{0} bắt buộc nhập")]
        [StringLength(20, MinimumLength = 6, ErrorMessage = "{0} phải từ {2} đến {1} ký tự")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Xác thực")]
        public bool EmailConfirmed { get; set; }
    }
}
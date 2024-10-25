using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebPhone.Models.Accounts
{
    public class EmailConfirmed
    {
        [Display(Name = "Email")]
        [Required(ErrorMessage = "{0} bắt buộc nhập")]
        public string Email { get; set; }

        [Display(Name = "Mã xác thực")]
        [Required(ErrorMessage = "{0} bắt buộc nhập")]
        public string Code { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebPhone.Models.Roles
{
    public class RoleDTO
    {
        public Guid Id { get; set; }

        [Display(Name = "Tên quyền")]
        [Required(ErrorMessage = "{0} bắt buộc nhập")]
        public string RoleName { get; set; }
    }
}
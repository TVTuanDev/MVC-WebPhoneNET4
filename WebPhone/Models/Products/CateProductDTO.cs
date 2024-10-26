using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebPhone.Models.Products
{
    public class CateProductDTO
    {
        public Guid Id { get; set; }

        [Display(Name = "Tên danh mục")]
        [Required(ErrorMessage = "{0} bắt buộc nhập")]
        public string CategoryName { get; set; }

        [Display(Name = "Danh mục cha")]
        public Guid? ParentId { get; set; }
    }
}
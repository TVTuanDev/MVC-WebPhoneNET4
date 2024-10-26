using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebPhone.Models.Products
{
    public class ProductDTO
    {
        public Guid Id { get; set; }

        [Display(Name = "Tên sản phẩm")]
        [Required(ErrorMessage = "{0} bắt buộc nhập")]
        public string ProductName { get; set; }

        [Display(Name = "Thông tin sản phẩm")]
        [Required(ErrorMessage = "{0} bắt buộc nhập")]
        public string Description { get; set; }

        [Display(Name = "Giá sản phẩm")]
        [Required(ErrorMessage = "{0} bắt buộc nhập")]
        [Range(0, int.MaxValue, ErrorMessage = "Giá trị phải lớn hơn 0")]
        public int Price { get; set; }

        [Display(Name = "Giá giảm")]
        [Range(0, int.MaxValue, ErrorMessage = "Giá trị phải lớn hơn 0")]
        public int? Discount { get; set; }

        [Display(Name = "Danh mục sản phẩm")]
        [Required(ErrorMessage = "{0} bắt buộc nhập")]
        public Guid? CategoryId { get; set; }
    }
}
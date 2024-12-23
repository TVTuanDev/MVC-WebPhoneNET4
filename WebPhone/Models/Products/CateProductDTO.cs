﻿using System;
using System.ComponentModel.DataAnnotations;

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
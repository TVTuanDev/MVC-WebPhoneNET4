﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebPhone.EF
{
    [Table("BillInfos")]
    public class BillInfo
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? ProductId { get; set; }
        public Guid BillId { get; set; }

        [Required]
        [StringLength(500)]
        public string ProductName { get; set; }
        public int Price { get; set; }
        public int? Discount { get; set; }
        public int Quantity { get; set; }
        public DateTime CreateAt { get; set; } = DateTime.Now;
        public DateTime? UpdateAt { get; set; }
        public virtual Product Product { get; set; }
        public virtual Bill Bill { get; set; }
    }
}
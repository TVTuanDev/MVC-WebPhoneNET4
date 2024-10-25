using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WebPhone.EF
{
    [Table("Products")]
    public class Product
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(500)]
        public string ProductName { get; set; }
        public string Avatar { get; set; }
        public string Description { get; set; }
        public int Price { get; set; }
        public int? Discount { get; set; }
        public DateTime CreateAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdateAt { get; set; }
        public Guid? CategoryId { get; set; }
        public virtual CategoryProduct CategoryProduct { get; set; }
        public virtual ICollection<BillInfo> BillInfos { get; set; } = new List<BillInfo>();
        public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    }
}
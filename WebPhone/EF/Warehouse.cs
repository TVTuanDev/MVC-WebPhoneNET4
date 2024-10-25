using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WebPhone.EF
{
    [Table("Warehouses")]
    public class Warehouse
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(200)]
        public string WarehouseName { get; set; }

        [Required]
        [StringLength(200)]
        public string Address { get; set; }
        public int Capacity { get; set; }
        public DateTime CreateAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdateAt { get; set; }
        public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    }
}
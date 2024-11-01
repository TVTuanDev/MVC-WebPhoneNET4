using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebPhone.EF
{
    [Table("Inventories")]
    public class Inventory
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ProductId { get; set; }
        public Guid WarehouseId { get; set; }
        public int Quantity { get; set; }
        public int ImportPrice { get; set; }
        public DateTime CreateAt { get; set; } = DateTime.Now;
        public DateTime? UpdateAt { get; set; }
        public virtual Product Product { get; set; }
        public virtual Warehouse Warehouse { get; set; }
    }
}
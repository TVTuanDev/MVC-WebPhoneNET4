using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WebPhone.EF
{
    [Table("CategoryProducts")]
    public class CategoryProduct
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; }
        public DateTime CreateAt { get; set; } = DateTime.Now;
        public DateTime? UpdateAt { get; set; }
        public Guid? ParentId { get; set; }
        public virtual CategoryProduct CateProductParent { get; set; }
        public virtual ICollection<CategoryProduct> CateProductChildren { get; set; } = new List<CategoryProduct>();
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
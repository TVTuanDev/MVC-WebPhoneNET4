using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebPhone.EF
{
    [Table("Bills")]
    public class Bill
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? CustomerId { get; set; }
        public Guid? EmploymentId { get; set; }

        [Required]
        [StringLength(200)]
        public string CustomerName { get; set; }

        [Required]
        [StringLength(200)]
        public string EmploymentName { get; set; }
        public int Price { get; set; }
        public DiscountStyle DiscountStyle { get; set; }
        public int DiscountStyleValue { get; set; }
        public int? Discount { get; set; }
        public int TotalPrice { get; set; }
        public int PaymentPrice { get; set; }
        public DateTime CreateAt { get; set; } = DateTime.Now;
        public DateTime? UpdateAt { get; set; }
        public virtual User Customer { get; set; }
        public virtual User Employment { get; set; }
        public virtual ICollection<BillInfo> BillInfos { get; set; } = new List<BillInfo>();
        public virtual ICollection<PaymentLog> PaymentLogs { get; set; } = new List<PaymentLog>();
    }

    public enum DiscountStyle
    {
        Percent,
        Money
    }
}
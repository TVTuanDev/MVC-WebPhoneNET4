using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebPhone.EF
{
    [Table("PaymentLogs")]
    public class PaymentLog
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid BillId { get; set; }
        public Guid CustomerId { get; set; }
        public int Price { get; set; }
        public DateTime CreateAt { get; set; } = DateTime.Now;
        public virtual Bill Bill { get; set; }
        public virtual User Customer { get; set; }
    }
}
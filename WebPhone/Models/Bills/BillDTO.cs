using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WebPhone.EF;

namespace WebPhone.Models.Bills
{
    public class BillDTO
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public DiscountStyle DiscountStyle { get; set; }
        public int DiscountValue { get; set; } = 0;
        public int PaymentValue { get; set; } = 0;
        public List<Guid?> ProductId { get; set; } = new List<Guid?>();
        public List<int> Quantities { get; set; } = new List<int>();
    }
}
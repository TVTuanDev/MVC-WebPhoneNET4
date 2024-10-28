using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebPhone.Models.Debts
{
    public class CustomerBillDTO
    {
        public DateTime BillDate { get; set; }
        public int? TotalPrice { get; set; }
        public int PaymentPrice { get; set; }
        public bool IsPayment { get; set; }
        public Guid BillId { get; set; }
    }
}
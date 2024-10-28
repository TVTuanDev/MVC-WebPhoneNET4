using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WebPhone.EF;

namespace WebPhone.Models.Debts
{
    public class PaymentDebtDTO
    {
        public Bill Bill { get; set; }
        public int PaymentTotal { get; set; }
    }
}
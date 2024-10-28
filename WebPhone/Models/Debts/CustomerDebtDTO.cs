using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WebPhone.EF;

namespace WebPhone.Models.Debts
{
    public class CustomerDebtDTO
    {
        public User Customer { get; set; }
        public int Debt { get; set; }
    }
}
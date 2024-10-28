using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WebPhone.EF;

namespace WebPhone.Models.Bills
{
    public class BillProductDTO
    {
        public Product Product { get; set; } = new Product();
        public int Quantity { get; set; }
    }
}
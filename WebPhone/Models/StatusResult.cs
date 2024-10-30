using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebPhone.Models
{
    public class StatusResult
    {
        public bool Succeeded { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
    }
}
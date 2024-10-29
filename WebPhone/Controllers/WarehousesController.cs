using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebPhone.EF;

namespace WebPhone.Controllers
{
    [RoutePrefix("warehouse")]
    public class WarehousesController : Controller
    {
        private readonly AppDbContext _context;

        public WarehousesController()
        {
            _context = new AppDbContext();
        }

        [HttpGet]
        [Route]
        public ActionResult Index()
        {
            return View();
        }
    }
}
using Microsoft.Data.SqlClient;
using System;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Web.Mvc;
using WebPhone.Attributes;
using WebPhone.EF;
using WebPhone.Models.Roles;

namespace WebPhone.Controllers
{
    [RoutePrefix("admin/role")]
    [AppAuthorize("Adminitrator")]
    public class RolesController : Controller
    {
        private readonly AppDbContext _context;

        public RolesController()
        {
            _context = new AppDbContext();
        }

        [HttpGet]
        [Route]
        public async Task<ActionResult> Index()
        {
            var roles = await _context.Roles.ToListAsync();
            return View(roles);
        }

        [HttpGet]
        [Route("details")]
        public async Task<ActionResult> Details(Guid? id)
        {
            var role = await _context.Roles
                .FirstOrDefaultAsync(m => m.Id == id);
            if (role == null)
            {
                TempData["Message"] = "Error: Không tìm thấy thông tin";
                return RedirectToAction(nameof(Index));
            }

            return View(role);
        }

        [HttpGet]
        [Route("create")]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Route("create")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(RoleDTO roleDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["Message"] = "Error: Vui lòng nhập đầy đủ thông tin";
                    return View(roleDTO);
                }

                var role = new Role
                {
                    RoleName = roleDTO.RoleName,
                };

                _context.Roles.Add(role);
                await _context.SaveChangesAsync();

                TempData["Message"] = "Success: Thêm mới quyền thành công";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                if (ex.InnerException is SqlException sqlEx)
                {
                    if (sqlEx.Number == 2601 || sqlEx.Number == 2627)
                    {
                        TempData["Message"] = "Error: Tên quyền đã được sử dụng";
                        return View(roleDTO);
                    }
                }

                Console.WriteLine(ex.Message);
                TempData["Message"] = "Error: Lỗi hệ thống";
                return View(roleDTO);
            }
        }

        [HttpGet]
        [Route("edit")]
        public async Task<ActionResult> Edit(Guid? id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                TempData["Message"] = "Error: Không tìm thấy thông tin quyền";
                return RedirectToAction(nameof(Index));
            }

            var roleDTO = new RoleDTO
            {
                Id = role.Id,
                RoleName = role.RoleName,
            };

            return View(roleDTO);
        }

        [HttpPost]
        [Route("edit")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, RoleDTO roleDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["Message"] = "Error: Vui lòng nhập đầy đủ thông tin";
                    return View(roleDTO);
                }

                var role = await _context.Roles.FindAsync(roleDTO.Id);
                if (role == null)
                {
                    TempData["Message"] = "Error: Không tìm thấy thông tin quyền";
                    return View(roleDTO);
                }

                role.RoleName = roleDTO.RoleName;
                role.UpdateAt = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["Message"] = "Success: Cập nhật thành công";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                if (ex.InnerException is SqlException sqlEx)
                {
                    if (sqlEx.Number == 2601 || sqlEx.Number == 2627)
                    {
                        TempData["Message"] = "Error: Tên quyền đã được sử dụng";
                        return View(roleDTO);
                    }
                }

                Console.WriteLine(ex.Message);
                TempData["Message"] = "Error: Lỗi hệ thống";
                return View(roleDTO);
            }
        }

        [HttpGet]
        [Route("delete")]
        public async Task<ActionResult> Delete(Guid? id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                TempData["Message"] = "Error: Không tìm thấy thông tin";
                return RedirectToAction(nameof(Index));
            }

            return View(role);
        }

        [HttpPost, ActionName("Delete")]
        [Route("delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(Guid id)
        {
            try
            {
                var role = await _context.Roles.FindAsync(id);
                if (role == null)
                {
                    TempData["Message"] = "Error: Không tìm thấy thông tin";
                    return RedirectToAction(nameof(Index));
                }

                _context.Roles.Remove(role);
                await _context.SaveChangesAsync();

                TempData["Message"] = "Success: Xóa quyền thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                TempData["Message"] = "Error: Lỗi hệ thống";
                return View();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose(); // Giải phóng DbContext
            }
            base.Dispose(disposing);
        }
    }
}
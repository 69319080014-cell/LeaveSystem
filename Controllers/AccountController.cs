using LeaveSystem.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeaveSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // ถ้าล็อกอินค้างไว้แล้ว ให้เด้งเข้าหน้าหลักทันที
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Index", "Leave");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string employeeCode)
        {
            if (string.IsNullOrWhiteSpace(employeeCode))
            {
                ViewBag.Error = "กรุณากรอกรหัสพนักงาน";
                return View();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.EmployeeCode.ToLower() == employeeCode.Trim().ToLower());

            if (user == null)
            {
                ViewBag.Error = "ไม่พบรหัสพนักงานนี้ในระบบ";
                return View();
            }

            // เก็บข้อมูลผู้ใช้ลง Session
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("EmployeeCode", user.EmployeeCode);
            HttpContext.Session.SetString("FullName", user.FullName);
            HttpContext.Session.SetString("Role", user.Role);

            return RedirectToAction("Index", "Leave");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
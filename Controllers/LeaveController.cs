using LeaveSystem.Data;
using LeaveSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeaveSystem.Controllers
{
    public class LeaveController : Controller
    {
        private readonly AppDbContext _context;

        public LeaveController(AppDbContext context)
        {
            _context = context;
        }

        private int? CurrentUserId => HttpContext.Session.GetInt32("UserId");
        private string CurrentUserRole => HttpContext.Session.GetString("Role") ?? "";

        // 1. หน้าภาพรวม & อนุมัติใบลา (แยกหน้าบ้าน Employee / หลังบ้าน Admin)
        public async Task<IActionResult> Index(int? filterUserId)
        {
            if (CurrentUserId == null) return RedirectToAction("Login", "Account");

            bool isAdmin = CurrentUserRole == "Admin";
            ViewBag.IsAdmin = isAdmin;
            ViewBag.SelectedUserId = filterUserId;
            ViewBag.Users = await _context.Users.ToListAsync();

            int currentYear = DateTime.Now.Year;
            int? targetUserId = isAdmin ? filterUserId : CurrentUserId;

            // ตรวจสอบและสร้างโควตาวันลาเริ่มต้นอัตโนมัติ หากพนักงานที่ล็อกอินอยู่ยังไม่มีข้อมูลในปีนี้
            if (CurrentUserId.HasValue)
            {
                bool hasBalance = await _context.LeaveBalances
                    .AnyAsync(b => b.UserId == CurrentUserId.Value && b.Year == currentYear);

                if (!hasBalance)
                {
                    _context.LeaveBalances.AddRange(
                        new LeaveBalance { UserId = CurrentUserId.Value, Year = currentYear, LeaveType = "ลาพักร้อน", TotalDays = 10 },
                        new LeaveBalance { UserId = CurrentUserId.Value, Year = currentYear, LeaveType = "ลากิจ", TotalDays = 6 },
                        new LeaveBalance { UserId = CurrentUserId.Value, Year = currentYear, LeaveType = "ลาป่วย", TotalDays = 30 }
                    );
                    await _context.SaveChangesAsync();
                }
            }

            var balanceQuery = _context.LeaveBalances
                .Include(b => b.User)
                .Where(b => b.Year == currentYear);

            var requestQuery = _context.LeaveRequests
                .Include(r => r.User)
                .Where(r => r.StartDate.Year == currentYear);

            if (targetUserId.HasValue)
            {
                balanceQuery = balanceQuery.Where(b => b.UserId == targetUserId.Value);
                requestQuery = requestQuery.Where(r => r.UserId == targetUserId.Value);
            }

            var balances = await balanceQuery.ToListAsync();
            var requests = await requestQuery.OrderByDescending(r => r.Id).ToListAsync();

            var approvedRequests = await _context.LeaveRequests
                .Where(r => r.Status == "Approved" && r.StartDate.Year == currentYear)
                .ToListAsync();

            // สร้างข้อมูลสรุปวันลาคงเหลือให้ตรงกับ ViewBag.BalanceSummary ใน Index.cshtml
            ViewBag.BalanceSummary = balances.Select(b =>
            {
                int used = approvedRequests
                    .Where(r => r.UserId == b.UserId && r.LeaveType == b.LeaveType)
                    .Sum(r => r.TotalDays);

                return new
                {
                    UserName = b.User?.FullName ?? "",
                    EmployeeCode = b.User?.EmployeeCode ?? "",
                    LeaveType = b.LeaveType,
                    Year = b.Year,
                    TotalDays = b.TotalDays,
                    UsedDays = used,
                    RemainingDays = b.TotalDays - used
                };
            }).ToList();

            return View(requests);
        }

        // 2. หน้ายื่นใบลาใหม่
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (CurrentUserId == null) return RedirectToAction("Login", "Account");

            ViewBag.IsAdmin = CurrentUserRole == "Admin";
            ViewBag.Users = CurrentUserRole == "Admin"
                ? await _context.Users.ToListAsync()
                : await _context.Users.Where(u => u.Id == CurrentUserId).ToListAsync();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(LeaveRequest model)
        {
            if (CurrentUserId == null) return RedirectToAction("Login", "Account");

            if (CurrentUserRole != "Admin")
            {
                model.UserId = CurrentUserId.Value;
            }

            if (model.EndDate < model.StartDate)
            {
                TempData["Error"] = "วันที่สิ้นสุดต้องไม่น้อยกว่าวันที่เริ่มต้น";
                return RedirectToAction("Create");
            }

            if (model.StartDate.Year != model.EndDate.Year)
            {
                TempData["Error"] = "ไม่อนุญาตให้ยื่นใบลาข้ามปี กรุณาแยกใบลาตามปีปฏิทิน";
                return RedirectToAction("Create");
            }

            int workingDays = 0;
            for (var date = model.StartDate.Date; date <= model.EndDate.Date; date = date.AddDays(1))
            {
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                {
                    workingDays++;
                }
            }

            if (workingDays <= 0)
            {
                TempData["Error"] = "ช่วงวันที่เลือกไม่มีวันทำงาน (ตรงกับวันเสาร์-อาทิตย์ทั้งหมด)";
                return RedirectToAction("Create");
            }

            var balance = await _context.LeaveBalances
                .FirstOrDefaultAsync(b => b.UserId == model.UserId && b.Year == model.StartDate.Year && b.LeaveType == model.LeaveType);

            if (balance == null)
            {
                TempData["Error"] = $"ไม่พบสิทธิการลาประเภท '{model.LeaveType}' ในปี {model.StartDate.Year}";
                return RedirectToAction("Create");
            }

            int usedDays = await _context.LeaveRequests
                .Where(r => r.UserId == model.UserId && r.LeaveType == model.LeaveType && r.Status == "Approved" && r.StartDate.Year == model.StartDate.Year)
                .SumAsync(r => (int?)r.TotalDays) ?? 0;

            if (balance.TotalDays - usedDays < workingDays)
            {
                TempData["Error"] = $"สิทธิวันลาคงเหลือไม่เพียงพอ (คงเหลือ {balance.TotalDays - usedDays} วัน, ขอลา {workingDays} วัน)";
                return RedirectToAction("Create");
            }

            model.TotalDays = workingDays;
            model.Status = "Pending";
            _context.LeaveRequests.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"ยื่นใบลาสำเร็จ ({workingDays} วันทำการ) สถานะ: รอพิจารณา";
            return RedirectToAction("Index");
        }

        // 3. เปลี่ยนสถานะใบลา (รับพารามิเตอร์ newStatus และ reviewerNote ตรงกับฟอร์มใน Index.cshtml)
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string newStatus, string? reviewerNote)
        {
            if (CurrentUserRole != "Admin")
            {
                TempData["Error"] = "คุณไม่มีสิทธิ์ดำเนินการนี้ (เฉพาะ Admin เท่านั้น)";
                return RedirectToAction("Index");
            }

            var request = await _context.LeaveRequests.FindAsync(id);
            if (request == null) return NotFound();

            if (newStatus == "Approved" && request.Status != "Approved")
            {
                var balance = await _context.LeaveBalances
                    .FirstOrDefaultAsync(b => b.UserId == request.UserId && b.Year == request.StartDate.Year && b.LeaveType == request.LeaveType);

                int usedDays = await _context.LeaveRequests
                    .Where(r => r.UserId == request.UserId && r.LeaveType == request.LeaveType && r.Status == "Approved" && r.StartDate.Year == request.StartDate.Year && r.Id != request.Id)
                    .SumAsync(r => (int?)r.TotalDays) ?? 0;

                if (balance == null || (balance.TotalDays - usedDays) < request.TotalDays)
                {
                    TempData["Error"] = "ไม่สามารถอนุมัติได้ เนื่องจากสิทธิวันลาคงเหลือของพนักงานไม่เพียงพอ";
                    return RedirectToAction("Index");
                }
            }

            request.Status = newStatus;
            request.ReviewerNote = reviewerNote;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"อัปเดตสถานะใบลา #{id} เรียบร้อยแล้ว ({newStatus})";
            return RedirectToAction("Index");
        }

        // 4. หน้าจัดการข้อมูลผู้ใช้และสิทธิวันลา (เฉพาะ Admin)
        [HttpGet]
        public async Task<IActionResult> ManageMaster()
        {
            if (CurrentUserId == null) return RedirectToAction("Login", "Account");
            if (CurrentUserRole != "Admin")
            {
                TempData["Error"] = "หน้านี้สำหรับผู้ดูแลระบบ (Admin) เท่านั้น";
                return RedirectToAction("Index");
            }

            ViewBag.Users = await _context.Users.ToListAsync();
            ViewBag.Balances = await _context.LeaveBalances.Include(b => b.User).OrderByDescending(b => b.Year).ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddUser(User user)
        {
            if (CurrentUserRole != "Admin") return RedirectToAction("Index");

            if (await _context.Users.AnyAsync(u => u.EmployeeCode == user.EmployeeCode))
            {
                TempData["Error"] = "รหัสพนักงานนี้มีอยู่ในระบบแล้ว";
                return RedirectToAction("ManageMaster");
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // สร้างสิทธิวันลาเริ่มต้นประจำปีให้อัตโนมัติทันทีที่เพิ่มพนักงานใหม่
            int currentYear = DateTime.Now.Year;
            _context.LeaveBalances.AddRange(
                new LeaveBalance { UserId = user.Id, Year = currentYear, LeaveType = "ลาพักร้อน", TotalDays = 10 },
                new LeaveBalance { UserId = user.Id, Year = currentYear, LeaveType = "ลากิจ", TotalDays = 6 },
                new LeaveBalance { UserId = user.Id, Year = currentYear, LeaveType = "ลาป่วย", TotalDays = 30 }
            );
            await _context.SaveChangesAsync();

            TempData["Success"] = $"เพิ่มพนักงาน {user.FullName} พร้อมกำหนดสิทธิวันลาเริ่มต้นเรียบร้อยแล้ว";
            return RedirectToAction("ManageMaster");
        }

        [HttpPost]
        public async Task<IActionResult> SaveBalance(LeaveBalance balance)
        {
            if (CurrentUserRole != "Admin") return RedirectToAction("Index");

            var existing = await _context.LeaveBalances
                .FirstOrDefaultAsync(b => b.UserId == balance.UserId && b.Year == balance.Year && b.LeaveType == balance.LeaveType);

            if (existing != null)
            {
                existing.TotalDays = balance.TotalDays;
            }
            else
            {
                _context.LeaveBalances.Add(balance);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "บันทึกสิทธิวันลาเรียบร้อยแล้ว";
            return RedirectToAction("ManageMaster");
        }
    }
}
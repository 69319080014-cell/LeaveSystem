using LeaveSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace LeaveSystem.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<LeaveBalance> LeaveBalances { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ข้อมูลผู้ใช้เริ่มต้น
            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, EmployeeCode = "EMP001", FullName = "สมชาย ใจดี", Department = "IT", Role = "Employee" },
                new User { Id = 2, EmployeeCode = "EMP002", FullName = "สมหญิง รักงาน", Department = "HR", Role = "Admin" }
            );

            // ข้อมูลสิทธิวันลาเริ่มต้น
            int currentYear = DateTime.Now.Year;
            modelBuilder.Entity<LeaveBalance>().HasData(
                new LeaveBalance { Id = 1, UserId = 1, Year = currentYear, LeaveType = "ลาพักร้อน", TotalDays = 6 },
                new LeaveBalance { Id = 2, UserId = 1, Year = currentYear, LeaveType = "ลากิจ", TotalDays = 3 },
                new LeaveBalance { Id = 3, UserId = 1, Year = currentYear, LeaveType = "ลาป่วย", TotalDays = 30 },
                new LeaveBalance { Id = 4, UserId = 2, Year = currentYear, LeaveType = "ลาพักร้อน", TotalDays = 10 }
            );
        }
    }
}
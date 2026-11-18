using System.ComponentModel.DataAnnotations;

namespace LeaveSystem.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "กรุณากรอกรหัสพนักงาน")]
        [Display(Name = "รหัสพนักงาน")]
        public string EmployeeCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณากรอกชื่อ-นามสกุล")]
        [Display(Name = "ชื่อ-นามสกุล")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "แผนก")]
        public string Department { get; set; } = string.Empty;

        [Display(Name = "บทบาท")]
        public string Role { get; set; } = "Employee"; // Employee หรือ Admin

        public ICollection<LeaveBalance> LeaveBalances { get; set; } = new List<LeaveBalance>();
        public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    }
}
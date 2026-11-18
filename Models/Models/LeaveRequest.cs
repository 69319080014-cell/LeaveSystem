using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeaveSystem.Models
{
    public class LeaveRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "พนักงาน")]
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required]
        [Display(Name = "ประเภทการลา")]
        public string LeaveType { get; set; } = "ลาพักร้อน";

        [Required(ErrorMessage = "กรุณาเลือกวันที่เริ่มลา")]
        [DataType(DataType.Date)]
        [Display(Name = "วันที่เริ่มลา")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "กรุณาเลือกวันที่สิ้นสุด")]
        [DataType(DataType.Date)]
        [Display(Name = "วันที่สิ้นสุด")]
        public DateTime EndDate { get; set; }

        [Display(Name = "จำนวนวันลา (จ.-ศ.)")]
        public int TotalDays { get; set; }

        [Required(ErrorMessage = "กรุณาระบุเหตุผลการลา")]
        [Display(Name = "เหตุผลการลา")]
        public string Reason { get; set; } = string.Empty;

        [Display(Name = "สถานะ")]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        [Display(Name = "หมายเหตุผู้พิจารณา")]
        public string? ReviewerNote { get; set; }

        [Display(Name = "วันที่ยื่นคำขอ")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeaveSystem.Models
{
    public class LeaveBalance
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "พนักงาน")]
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required]
        [Display(Name = "ปีปฏิทิน")]
        public int Year { get; set; } = DateTime.Now.Year;

        [Required]
        [Display(Name = "ประเภทการลา")]
        public string LeaveType { get; set; } = "ลาพักร้อน";

        [Required]
        [Range(0, 365)]
        [Display(Name = "สิทธิวันลาทั้งหมด (วัน)")]
        public int TotalDays { get; set; }
    }
}
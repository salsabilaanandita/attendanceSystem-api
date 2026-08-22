using System;

namespace AttendanceSystem.API.Models
{
    public class Attendance
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid? ShiftId { get; set; }
        public DateOnly AttendanceDate { get; set; }
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public string Status { get; set; } = string.Empty; // Present/Late/Absent/Leave/Sick
        public decimal? LatitudeIn { get; set; }
        public decimal? LongitudeIn { get; set; }
        public string? ImageUrl { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public Employee Employee { get; set; } = null!;
        public Shift? Shift { get; set; }
    }
}
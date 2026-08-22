using System;

namespace AttendanceSystem.API.Models
{
    public class Leave
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string Type { get; set; } = string.Empty; // cuti, sakit, izin
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? AttachmentUrl { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        public Guid? ApprovedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public Employee Employee { get; set; } = null!;
        public User? Approver { get; set; }
    }
}
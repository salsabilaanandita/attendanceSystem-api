using System;
using System.Collections.Generic;

namespace AttendanceSystem.API.Models
{
    public class Employee
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; } // nullable, karyawan bisa belum punya akun login
        public Guid? DepartmentId { get; set; } // nullable sesuai skema baru (ON DELETE SET NULL)
        public string? Nik { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Position { get; set; }
        public DateOnly JoinDate { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
        public Department? Department { get; set; }
        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public ICollection<Leave> Leaves { get; set; } = new List<Leave>();
    }
}
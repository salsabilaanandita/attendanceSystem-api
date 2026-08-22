using System;

namespace AttendanceSystem.API.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public Guid RoleId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public Role Role { get; set; } = null!;
        public Employee? Employee { get; set; } // reverse nav, FK ada di Employee sekarang
    }
}
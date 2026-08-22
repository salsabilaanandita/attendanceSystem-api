using System;
using System.Collections.Generic;

namespace AttendanceSystem.API.Models
{
    public class Department
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
}
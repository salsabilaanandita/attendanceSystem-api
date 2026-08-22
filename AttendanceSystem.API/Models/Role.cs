using System;
using System.Collections.Generic;

namespace AttendanceSystem.API.Models
{
    public class Role
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
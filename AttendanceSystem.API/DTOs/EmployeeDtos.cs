namespace AttendanceSystem.API.DTOs
{
    public class EmployeeDto
    {
        public Guid Id { get; set; }
        public string? Nik { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Position { get; set; }
        public DateOnly JoinDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public Guid? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
    }

    public class CreateEmployeeDto
    {
        public string? Nik { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Position { get; set; }
        public DateOnly JoinDate { get; set; }
        public Guid? DepartmentId { get; set; }
    }

    public class UpdateEmployeeDto
    {
        public string? Nik { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Position { get; set; }
        public Guid? DepartmentId { get; set; }
    }
}
namespace AttendanceSystem.API.DTOs
{
    public class LeaveDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? AttachmentUrl { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ApprovedByEmail { get; set; }
    }

    public class CreateLeaveDto
    {
        public string Type { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? AttachmentUrl { get; set; }
    }

    public class LeaveDecisionDto
    {
        public string? Note { get; set; }
    }

    public class LeaveFilterDto
    {
        public Guid? EmployeeId { get; set; }
        public string? Status { get; set; }
        public string? Type { get; set; }
    }
}
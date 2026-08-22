namespace AttendanceSystem.API.DTOs
{
    public class ShiftDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public int GracePeriodMinutes { get; set; }
    }

    public class CreateShiftDto
    {
        public string Name { get; set; } = string.Empty;
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public int GracePeriodMinutes { get; set; } = 0;
    }

    public class UpdateShiftDto
    {
        public string Name { get; set; } = string.Empty;
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public int GracePeriodMinutes { get; set; } = 0;
    }
}
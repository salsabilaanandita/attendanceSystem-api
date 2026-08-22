using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AttendanceSystem.API.Data;
using AttendanceSystem.API.DTOs;
using AttendanceSystem.API.Models;
using AttendanceSystem.API.Services;

namespace AttendanceSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AttendanceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly CurrentUserService _currentUser;

        public AttendanceController(ApplicationDbContext context, CurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        // POST: api/attendance/checkin
        [HttpPost("checkin")]
        public async Task<IActionResult> CheckIn(CheckInDto dto)
        {
            var employeeId = _currentUser.GetEmployeeId();
            if (employeeId == null)
                return BadRequest(new { message = "Akun ini tidak terhubung dengan data karyawan" });

            var today = DateOnly.FromDateTime(DateTime.Now);

            var existing = await _context.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.AttendanceDate == today);

            if (existing != null)
                return BadRequest(new { message = "Anda sudah check-in hari ini" });

            var now = DateTime.Now;

            var standardCheckIn = today.ToDateTime(new TimeOnly(8, 0));
            var tolerance = TimeSpan.FromMinutes(15);
            var status = now <= standardCheckIn.Add(tolerance) ? "Present" : "Late";

            var attendance = new Attendance
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId.Value,
                AttendanceDate = today,
                CheckIn = now.ToUniversalTime(),
                Status = status,
                Notes = dto.Note,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Check-in berhasil", checkInTime = now, status });
        }

        // POST: api/attendance/checkout
        [HttpPost("checkout")]
        public async Task<IActionResult> CheckOut(CheckOutDto dto)
        {
            var employeeId = _currentUser.GetEmployeeId();
            if (employeeId == null)
                return BadRequest(new { message = "Akun ini tidak terhubung dengan data karyawan" });

            var today = DateOnly.FromDateTime(DateTime.Now);

            var attendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.AttendanceDate == today);

            if (attendance == null)
                return BadRequest(new { message = "Anda belum check-in hari ini" });

            if (attendance.CheckOut != null)
                return BadRequest(new { message = "Anda sudah check-out hari ini" });

            attendance.CheckOut = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(dto.Note))
                attendance.Notes = dto.Note;
            attendance.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Check-out berhasil", checkOutTime = attendance.CheckOut });
        }

        // GET: api/attendance/today
        [HttpGet("today")]
        public async Task<IActionResult> GetToday()
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var role = _currentUser.GetRole();

            var query = _context.Attendances
                .Include(a => a.Employee)
                .ThenInclude(e => e.Department)
                .Where(a => a.AttendanceDate == today);

            if (role == "Employee")
            {
                var employeeId = _currentUser.GetEmployeeId();
                query = query.Where(a => a.EmployeeId == employeeId);
            }

            var result = await query
                .Select(a => new AttendanceDto
                {
                    Id = a.Id,
                    EmployeeId = a.EmployeeId,
                    EmployeeName = a.Employee.Name,
                    DepartmentName = a.Employee.Department != null ? a.Employee.Department.Name : null,
                    AttendanceDate = a.AttendanceDate,
                    CheckIn = a.CheckIn,
                    CheckOut = a.CheckOut,
                    Status = a.Status,
                    Notes = a.Notes
                })
                .ToListAsync();

            return Ok(result);
        }

        // GET: api/attendance/history
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] AttendanceFilterDto filter)
        {
            var role = _currentUser.GetRole();

            var query = _context.Attendances
                .Include(a => a.Employee)
                .ThenInclude(e => e.Department)
                .AsQueryable();

            if (role == "Employee")
            {
                var employeeId = _currentUser.GetEmployeeId();
                query = query.Where(a => a.EmployeeId == employeeId);
            }
            else if (filter.EmployeeId.HasValue)
            {
                query = query.Where(a => a.EmployeeId == filter.EmployeeId.Value);
            }

            if (filter.StartDate.HasValue)
                query = query.Where(a => a.AttendanceDate >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(a => a.AttendanceDate <= filter.EndDate.Value);

            if (!string.IsNullOrWhiteSpace(filter.Status))
                query = query.Where(a => a.Status == filter.Status);

            var result = await query
                .OrderByDescending(a => a.AttendanceDate)
                .Select(a => new AttendanceDto
                {
                    Id = a.Id,
                    EmployeeId = a.EmployeeId,
                    EmployeeName = a.Employee.Name,
                    DepartmentName = a.Employee.Department != null ? a.Employee.Department.Name : null,
                    AttendanceDate = a.AttendanceDate,
                    CheckIn = a.CheckIn,
                    CheckOut = a.CheckOut,
                    Status = a.Status,
                    Notes = a.Notes
                })
                .ToListAsync();

            return Ok(result);
        }

        // GET: api/attendance/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var attendance = await _context.Attendances
                .Include(a => a.Employee)
                .ThenInclude(e => e.Department)
                .Where(a => a.Id == id)
                .Select(a => new AttendanceDto
                {
                    Id = a.Id,
                    EmployeeId = a.EmployeeId,
                    EmployeeName = a.Employee.Name,
                    DepartmentName = a.Employee.Department != null ? a.Employee.Department.Name : null,
                    AttendanceDate = a.AttendanceDate,
                    CheckIn = a.CheckIn,
                    CheckOut = a.CheckOut,
                    Status = a.Status,
                    Notes = a.Notes
                })
                .FirstOrDefaultAsync();

            if (attendance == null)
                return NotFound(new { message = "Data absensi tidak ditemukan" });

            return Ok(attendance);
        }
    }
}
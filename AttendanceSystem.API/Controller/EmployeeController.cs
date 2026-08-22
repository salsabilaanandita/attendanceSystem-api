using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AttendanceSystem.API.Data;
using AttendanceSystem.API.DTOs;
using AttendanceSystem.API.Models;

namespace AttendanceSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // wajib login untuk semua endpoint di controller ini
    public class EmployeeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EmployeeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/employee?search=budi&departmentId=xxx&status=Active
        // Cuma Admin & HR yang boleh lihat daftar semua karyawan
        [HttpGet]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search,
            [FromQuery] Guid? departmentId,
            [FromQuery] string? status)
        {
            var query = _context.Employees.Include(e => e.Department).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(e => e.Name.Contains(search) || (e.Nik != null && e.Nik.Contains(search)));

            if (departmentId.HasValue)
                query = query.Where(e => e.DepartmentId == departmentId.Value);

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(e => e.Status == status);

            var employees = await query
                .Select(e => new EmployeeDto
                {
                    Id = e.Id,
                    Nik = e.Nik,
                    Name = e.Name,
                    Phone = e.Phone,
                    Position = e.Position,
                    JoinDate = e.JoinDate,
                    Status = e.Status,
                    DepartmentId = e.DepartmentId,
                    DepartmentName = e.Department != null ? e.Department.Name : null
                })
                .ToListAsync();

            return Ok(employees);
        }

        // GET: api/employee/{id}
        // Admin & HR bisa lihat siapa saja; Employee cuma boleh lihat dirinya sendiri
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            if (role == "Employee")
            {
                var myEmployeeId = User.FindFirst("EmployeeId")?.Value;
                if (myEmployeeId == null || myEmployeeId != id.ToString())
                    return Forbid();
            }

            var employee = await _context.Employees
                .Include(e => e.Department)
                .Where(e => e.Id == id)
                .Select(e => new EmployeeDto
                {
                    Id = e.Id,
                    Nik = e.Nik,
                    Name = e.Name,
                    Phone = e.Phone,
                    Position = e.Position,
                    JoinDate = e.JoinDate,
                    Status = e.Status,
                    DepartmentId = e.DepartmentId,
                    DepartmentName = e.Department != null ? e.Department.Name : null
                })
                .FirstOrDefaultAsync();

            if (employee == null)
                return NotFound(new { message = "Karyawan tidak ditemukan" });

            return Ok(employee);
        }

        // POST: api/employee
        // Cuma Admin & HR yang boleh bikin data karyawan baru
        [HttpPost]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Create(CreateEmployeeDto dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.Nik) &&
                await _context.Employees.AnyAsync(e => e.Nik == dto.Nik))
                return BadRequest(new { message = "NIK sudah digunakan karyawan lain" });

            Department? department = null;
            if (dto.DepartmentId.HasValue)
            {
                department = await _context.Departments.FindAsync(dto.DepartmentId.Value);
                if (department == null)
                    return BadRequest(new { message = "Departemen tidak valid" });
            }

            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                Nik = dto.Nik,
                Name = dto.Name,
                Phone = dto.Phone,
                Position = dto.Position,
                JoinDate = dto.JoinDate,
                DepartmentId = dto.DepartmentId,
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            var resultDto = new EmployeeDto
            {
                Id = employee.Id,
                Nik = employee.Nik,
                Name = employee.Name,
                Phone = employee.Phone,
                Position = employee.Position,
                JoinDate = employee.JoinDate,
                Status = employee.Status,
                DepartmentId = employee.DepartmentId,
                DepartmentName = department?.Name
            };

            return CreatedAtAction(nameof(GetById), new { id = employee.Id }, resultDto);
        }

        // PUT: api/employee/{id}
        // Cuma Admin & HR yang boleh update
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Update(Guid id, UpdateEmployeeDto dto)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
                return NotFound(new { message = "Karyawan tidak ditemukan" });

            if (dto.DepartmentId.HasValue)
            {
                var department = await _context.Departments.FindAsync(dto.DepartmentId.Value);
                if (department == null)
                    return BadRequest(new { message = "Departemen tidak valid" });
            }

            employee.Nik = dto.Nik;
            employee.Name = dto.Name;
            employee.Phone = dto.Phone;
            employee.Position = dto.Position;
            employee.DepartmentId = dto.DepartmentId;
            employee.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Data karyawan berhasil diupdate" });
        }

        // PATCH: api/employee/{id}/deactivate
        // Cuma Admin & HR
        [HttpPatch("{id}/deactivate")]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
                return NotFound(new { message = "Karyawan tidak ditemukan" });

            employee.Status = "Inactive";
            employee.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Karyawan berhasil dinonaktifkan" });
        }

        // PATCH: api/employee/{id}/activate
        // Cuma Admin & HR
        [HttpPatch("{id}/activate")]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Activate(Guid id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
                return NotFound(new { message = "Karyawan tidak ditemukan" });

            employee.Status = "Active";
            employee.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Karyawan berhasil diaktifkan" });
        }
    }
}
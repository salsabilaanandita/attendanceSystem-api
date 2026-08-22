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
    public class DepartmentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DepartmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/department
        // Semua role yang login boleh lihat (Admin, HR, Employee)
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var departments = await _context.Departments
                .Select(d => new DepartmentDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    Description = d.Description,
                    EmployeeCount = d.Employees.Count
                })
                .ToListAsync();

            return Ok(departments);
        }

        // GET: api/department/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var department = await _context.Departments
                .Where(d => d.Id == id)
                .Select(d => new DepartmentDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    Description = d.Description,
                    EmployeeCount = d.Employees.Count
                })
                .FirstOrDefaultAsync();

            if (department == null)
                return NotFound(new { message = "Departemen tidak ditemukan" });

            return Ok(department);
        }

        // POST: api/department
        // Cuma Admin & HR yang boleh bikin department baru
        [HttpPost]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Create(CreateDepartmentDto dto)
        {
            if (await _context.Departments.AnyAsync(d => d.Name == dto.Name))
                return BadRequest(new { message = "Nama departemen sudah ada" });

            var department = new Department
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow
            };

            _context.Departments.Add(department);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = department.Id }, department);
        }

        // PUT: api/department/{id}
        // Cuma Admin & HR yang boleh update
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Update(Guid id, UpdateDepartmentDto dto)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
                return NotFound(new { message = "Departemen tidak ditemukan" });

            department.Name = dto.Name;
            department.Description = dto.Description;
            department.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Departemen berhasil diupdate" });
        }

        // DELETE: api/department/{id}
        // Cuma Admin yang boleh hapus (lebih ketat dari Create/Update)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var department = await _context.Departments
                .Include(d => d.Employees)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (department == null)
                return NotFound(new { message = "Departemen tidak ditemukan" });

            if (department.Employees.Any())
                return BadRequest(new { message = "Tidak bisa hapus departemen yang masih punya karyawan" });

            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Departemen berhasil dihapus" });
        }
    }
}
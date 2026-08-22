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
    [Authorize]
    public class ShiftController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ShiftController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/shift
        // Semua role boleh lihat daftar shift (buat referensi jam kerja)
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var shifts = await _context.Shifts
                .Select(s => new ShiftDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    GracePeriodMinutes = s.GracePeriodMinutes
                })
                .ToListAsync();

            return Ok(shifts);
        }

        // GET: api/shift/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var shift = await _context.Shifts
                .Where(s => s.Id == id)
                .Select(s => new ShiftDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    GracePeriodMinutes = s.GracePeriodMinutes
                })
                .FirstOrDefaultAsync();

            if (shift == null)
                return NotFound(new { message = "Shift tidak ditemukan" });

            return Ok(shift);
        }

        // POST: api/shift
        // Cuma Admin & HR
        [HttpPost]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Create(CreateShiftDto dto)
        {
            if (dto.EndTime <= dto.StartTime)
                return BadRequest(new { message = "Jam selesai harus lebih besar dari jam mulai" });

            var shift = new Shift
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                GracePeriodMinutes = dto.GracePeriodMinutes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Shifts.Add(shift);
            await _context.SaveChangesAsync();

            var resultDto = new ShiftDto
            {
                Id = shift.Id,
                Name = shift.Name,
                StartTime = shift.StartTime,
                EndTime = shift.EndTime,
                GracePeriodMinutes = shift.GracePeriodMinutes
            };

            return CreatedAtAction(nameof(GetById), new { id = shift.Id }, resultDto);
        }

        // PUT: api/shift/{id}
        // Cuma Admin & HR
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Update(Guid id, UpdateShiftDto dto)
        {
            var shift = await _context.Shifts.FindAsync(id);
            if (shift == null)
                return NotFound(new { message = "Shift tidak ditemukan" });

            if (dto.EndTime == dto.StartTime)
            return BadRequest(new { message = "Jam mulai dan selesai tidak boleh sama" });
                
            shift.Name = dto.Name;
            shift.StartTime = dto.StartTime;
            shift.EndTime = dto.EndTime;
            shift.GracePeriodMinutes = dto.GracePeriodMinutes;
            shift.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Shift berhasil diupdate" });
        }

        // DELETE: api/shift/{id}
        // Cuma Admin
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var shift = await _context.Shifts.FindAsync(id);
            if (shift == null)
                return NotFound(new { message = "Shift tidak ditemukan" });

            var isUsed = await _context.Attendances.AnyAsync(a => a.ShiftId == id);
            if (isUsed)
                return BadRequest(new { message = "Tidak bisa hapus shift yang masih dipakai di data absensi" });

            _context.Shifts.Remove(shift);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Shift berhasil dihapus" });
        }
    }
}
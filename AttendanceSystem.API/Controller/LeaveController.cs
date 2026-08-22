using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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
    public class LeaveController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly CurrentUserService _currentUser;

        private static readonly string[] ValidTypes = { "Cuti", "Sakit", "Izin" };

        public LeaveController(ApplicationDbContext context, CurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        // POST: api/leave
        // Employee mengajukan cuti/izin/sakit untuk dirinya sendiri
        [HttpPost]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> Create(CreateLeaveDto dto)
        {
            var employeeId = _currentUser.GetEmployeeId();
            if (employeeId == null)
                return BadRequest(new { message = "Akun ini tidak terhubung dengan data karyawan" });

            if (!ValidTypes.Contains(dto.Type))
                return BadRequest(new { message = "Tipe pengajuan tidak valid. Gunakan: Cuti, Sakit, atau Izin" });

            if (dto.EndDate < dto.StartDate)
                return BadRequest(new { message = "Tanggal selesai tidak boleh sebelum tanggal mulai" });

            var leave = new Leave
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId.Value,
                Type = dto.Type,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Reason = dto.Reason,
                AttachmentUrl = dto.AttachmentUrl,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Leaves.Add(leave);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Pengajuan berhasil dikirim, menunggu persetujuan", leaveId = leave.Id });
        }

        // GET: api/leave
        // Employee lihat pengajuan miliknya sendiri; Admin/HR lihat semua + filter
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] LeaveFilterDto filter)
        {
            var role = _currentUser.GetRole();

            var query = _context.Leaves
                .Include(l => l.Employee)
                .Include(l => l.Approver)
                .AsQueryable();

            if (role == "Employee")
            {
                var employeeId = _currentUser.GetEmployeeId();
                query = query.Where(l => l.EmployeeId == employeeId);
            }
            else if (filter.EmployeeId.HasValue)
            {
                query = query.Where(l => l.EmployeeId == filter.EmployeeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Status))
                query = query.Where(l => l.Status == filter.Status);

            if (!string.IsNullOrWhiteSpace(filter.Type))
                query = query.Where(l => l.Type == filter.Type);

            var result = await query
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new LeaveDto
                {
                    Id = l.Id,
                    EmployeeId = l.EmployeeId,
                    EmployeeName = l.Employee.Name,
                    Type = l.Type,
                    StartDate = l.StartDate,
                    EndDate = l.EndDate,
                    Reason = l.Reason,
                    AttachmentUrl = l.AttachmentUrl,
                    Status = l.Status,
                    ApprovedByEmail = l.Approver != null ? l.Approver.Email : null
                })
                .ToListAsync();

            return Ok(result);
        }

        // GET: api/leave/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var role = _currentUser.GetRole();

            var query = _context.Leaves
                .Include(l => l.Employee)
                .Include(l => l.Approver)
                .Where(l => l.Id == id);

            if (role == "Employee")
            {
                var employeeId = _currentUser.GetEmployeeId();
                query = query.Where(l => l.EmployeeId == employeeId);
            }

            var leave = await query
                .Select(l => new LeaveDto
                {
                    Id = l.Id,
                    EmployeeId = l.EmployeeId,
                    EmployeeName = l.Employee.Name,
                    Type = l.Type,
                    StartDate = l.StartDate,
                    EndDate = l.EndDate,
                    Reason = l.Reason,
                    AttachmentUrl = l.AttachmentUrl,
                    Status = l.Status,
                    ApprovedByEmail = l.Approver != null ? l.Approver.Email : null
                })
                .FirstOrDefaultAsync();

            if (leave == null)
                return NotFound(new { message = "Pengajuan tidak ditemukan" });

            return Ok(leave);
        }

        // PATCH: api/leave/{id}/approve
        // Cuma Admin & HR
        [HttpPatch("{id}/approve")]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Approve(Guid id)
        {
            var leave = await _context.Leaves.FindAsync(id);
            if (leave == null)
                return NotFound(new { message = "Pengajuan tidak ditemukan" });

            if (leave.Status != "Pending")
                return BadRequest(new { message = $"Pengajuan ini sudah berstatus '{leave.Status}', tidak bisa diproses ulang" });

            var approverIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(approverIdStr, out var approverId))
                return BadRequest(new { message = "Gagal mengidentifikasi akun approver" });

            leave.Status = "Approved";
            leave.ApprovedBy = approverId;
            leave.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Pengajuan berhasil disetujui" });
        }

        // PATCH: api/leave/{id}/reject
        // Cuma Admin & HR
        [HttpPatch("{id}/reject")]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Reject(
            Guid id,
            [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] LeaveDecisionDto? dto)
        {
            var leave = await _context.Leaves.FindAsync(id);
            if (leave == null)
                return NotFound(new { message = "Pengajuan tidak ditemukan" });

            if (leave.Status != "Pending")
                return BadRequest(new { message = $"Pengajuan ini sudah berstatus '{leave.Status}', tidak bisa diproses ulang" });

            var approverIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(approverIdStr, out var approverId))
                return BadRequest(new { message = "Gagal mengidentifikasi akun approver" });

            leave.Status = "Rejected";
            leave.ApprovedBy = approverId;
            if (!string.IsNullOrWhiteSpace(dto?.Note))
                leave.Reason += $" [Ditolak: {dto.Note}]";
            leave.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Pengajuan berhasil ditolak" });
        }
    }
}
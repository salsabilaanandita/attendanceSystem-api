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
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtService _jwtService;

        public AuthController(ApplicationDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequestDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest(new { message = "Email sudah terdaftar" });

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == dto.RoleName);
            if (role == null)
                return BadRequest(new { message = "Role tidak valid" });

            Employee? employee = null;

            if (dto.EmployeeId.HasValue)
            {
                employee = await _context.Employees.FindAsync(dto.EmployeeId.Value);
                if (employee == null)
                    return BadRequest(new { message = "Employee tidak ditemukan" });

                if (employee.UserId != null)
                    return BadRequest(new { message = "Employee ini sudah punya akun login" });
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                RoleId = role.Id,
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);

            if (employee != null)
            {
                employee.UserId = user.Id;
                employee.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Registrasi berhasil" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDto dto)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized(new { message = "Email atau password salah" });

            if (user.Status != "Active")
                return Unauthorized(new { message = "Akun tidak aktif" });

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserId == user.Id);

            var token = _jwtService.GenerateToken(user, user.Role.Name, employee?.Id);

            var response = new LoginResponseDto
            {
                Token = token,
                Email = user.Email,
                Role = user.Role.Name,
                EmployeeId = employee?.Id,
                EmployeeName = employee?.Name
            };

            return Ok(response);
        }
    }
}
using System.Security.Claims;

namespace AttendanceSystem.API.Services
{
    public class CurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? GetEmployeeId()
        {
            var value = _httpContextAccessor.HttpContext?.User?.FindFirst("EmployeeId")?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }

        public string? GetRole()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;
        }

        public string? GetEmail()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;
        }
    }
}
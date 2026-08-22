using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AttendanceSystem.API.Data;
using AttendanceSystem.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddOpenApi();

// --- Bikin Connection String yang Aman buat Npgsql (Railway / Local) ---
var rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
string connectionString;

if (!string.IsNullOrEmpty(rawConnectionString) && rawConnectionString.StartsWith("postgresql://"))
{
    // Jika format URL dari Railway (postgresql://user:pass@host:port/db)
    var uri = new Uri(rawConnectionString);
    var userInfo = uri.UserInfo.Split(':');
    var user = userInfo[0];
    var password = userInfo.Length > 1 ? userInfo[1] : "";
    var host = uri.Host;
    var port = uri.Port > 0 ? uri.Port : 5432;
    var database = uri.AbsolutePath.TrimStart('/');

    connectionString = $"Host={host};Port={port};Database={database};Username={user};Password={password};";
}
else
{
    // Jika format biasa (Host=localhost;Database=...) dari appsettings.json
    connectionString = rawConnectionString!;
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<JwtService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUserService>();   

var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Auto Apply Migration & Seed Roles saat aplikasi start
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    var defaultRoles = new[] { "Admin", "Employee" };
    foreach (var roleName in defaultRoles)
    {
        var exists = await db.Roles.AnyAsync(r => r.Name.ToLower() == roleName.ToLower());
        if (!exists)
        {
            db.Roles.Add(new AttendanceSystem.API.Models.Role
            {
                Id = Guid.NewGuid(),
                Name = roleName,
                Description = roleName == "Admin" ? "Administrator" : "Employee"
            });
        }
    }

    await db.SaveChangesAsync();
}

// Buka OpenAPI / Swagger di semua environment (termasuk Production di Railway)
app.MapOpenApi();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
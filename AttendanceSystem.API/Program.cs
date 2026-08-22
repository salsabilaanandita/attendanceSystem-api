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

// --- 1. AMBIL DAN PARSE DATABASE_URL (RAILWAY / LOCAL) ---
var envConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
var rawConnectionString = !string.IsNullOrEmpty(envConnectionString) 
    ? envConnectionString 
    : builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(rawConnectionString))
{
    throw new InvalidOperationException("CRITICAL ERROR: Connection string 'DATABASE_URL' or 'DefaultConnection' was not found in environment variables or appsettings.json.");
}

string connectionString;

// Tangani format URL postgresql:// atau postgres:// dari Railway
if (rawConnectionString.StartsWith("postgresql://") || rawConnectionString.StartsWith("postgres://"))
{
    var uri = new Uri(rawConnectionString);
    var userInfo = uri.UserInfo.Split(':');
    var user = userInfo[0];
    var password = userInfo.Length > 1 ? userInfo[1] : "";
    var host = uri.Host;
    var port = uri.Port > 0 ? uri.Port : 5432;
    var database = uri.AbsolutePath.TrimStart('/');

    connectionString = $"Host={host};Port={port};Database={database};Username={user};Password={password};Ssl Mode=Require;Trust Server Certificate=true;";
}
else
{
    connectionString = rawConnectionString;
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// --- 2. INJEKSI SERVICES ---
builder.Services.AddScoped<JwtService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUserService>();   

// --- 3. KONFIGURASI JWT ---
var jwtKey = builder.Configuration["Jwt:Key"] 
    ?? builder.Configuration["Jwt__Key"] 
    ?? Environment.GetEnvironmentVariable("Jwt__Key")
    ?? "AttendanceSystem_JWT_2026_SuperSecretKey_9xK7mP2qL8vN4rT6";

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
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "AttendanceSystemAPI",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "AttendanceSystemUser",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// --- 4. AUTO MIGRATION & SEEDING ROLE ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    // Jalankan auto migration ke PostgreSQL
    await db.Database.MigrateAsync();

    // Seed default roles
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

app.MapOpenApi();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
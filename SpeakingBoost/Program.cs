using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SpeakingBoost.Models.EF;
using SpeakingBoost.Services;
using SpeakingBoost.Services.Email;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1. CONTROLLERS + SWAGGER
// ============================================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger hỗ trợ nhập JWT Bearer token để test
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title   = "SpeakingBoost API",
        Version = "v1",
        Description = "RESTful API cho hệ thống luyện IELTS Speaking"
    });

    // Cho phép nhập Bearer token trong Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.ApiKey,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Nhập token theo dạng: Bearer {token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ============================================================
// 2. DATABASE — SpeakingBoostDB (database mới riêng)
// ============================================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")),
    ServiceLifetime.Scoped
);

// ============================================================
// 3. JWT AUTHENTICATION
// Thay thế hoàn toàn Cookie Authentication của MVC cũ
// Client gửi: Authorization: Bearer <token>
// ============================================================
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = builder.Configuration["Jwt:Issuer"],
        ValidAudience            = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

// ============================================================
// 4. CORS — cho phép frontend HTML/JS gọi API
// ============================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// ============================================================
// 5. ĐĂNG KÝ SERVICES (DI)
// ============================================================
builder.Services.AddScoped<ILoginServices, LoginServices>();
builder.Services.AddScoped<IJwtService,    JwtService>();
builder.Services.AddScoped<IEmailService,  EmailService>();

// ============================================================
// 6. BUILD
// ============================================================
var app = builder.Build();

// ============================================================
// 7. PIPELINE
// ============================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SpeakingBoost API v1");
        c.RoutePrefix = "swagger"; // Truy cập: https://localhost:PORT/swagger
    });
}

app.UseHttpsRedirection();

// Phục vụ file HTML/JS/CSS trong wwwroot (login.html, dashboard.html...)
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors("AllowAll");

app.UseAuthentication(); // ← PHẢI trước UseAuthorization
app.UseAuthorization();

app.MapControllers();

// ============================================================
// 8. TỰ ĐỘNG MIGRATE DATABASE KHI CHẠY
// ============================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger   = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();

        // ── Seed tài khoản mẫu (chỉ chạy trong Development) ──────────
        // student1@example.com / teacher1@example.com / admin1@example.com
        // Mật khẩu chung: Password123
        if (app.Environment.IsDevelopment())
        {
            DbSeeder.Seed(context, logger);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Lỗi khi tự động migrate database SpeakingBoostDB.");
    }
}

app.Run();
using SpeakingBoost.Services.Interfaces.Speaking;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SpeakingBoost.Models.EF;
using SpeakingBoost.Services.Interfaces.Auth;
using SpeakingBoost.Services.Implementations.Auth;
using SpeakingBoost.Services.Interfaces.Email;
using SpeakingBoost.Services.Implementations.Email;
using SpeakingBoost.Repositories.Interfaces.Student;
using SpeakingBoost.Repositories.Implementations.Student;
using SpeakingBoost.Services.Interfaces.Student;
using SpeakingBoost.Services.Implementations.Student;
using SpeakingBoost.Repositories.Interfaces.Admin;
using SpeakingBoost.Repositories.Implementations.Admin;
using SpeakingBoost.Services.Interfaces.Admin;
using SpeakingBoost.Services.Implementations.Admin;
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
builder.Services.AddHttpClient();

builder.Services.AddScoped<ILoginServices, LoginServices>();
builder.Services.AddScoped<IJwtService,    JwtService>();
builder.Services.AddScoped<IEmailService,  EmailService>();
builder.Services.AddScoped<IProfileService, ProfileService>();

// AI Speaking & Background Services
builder.Services.AddSingleton<SpeakingBoost.Services.Implementations.Background.BackgroundQueue>();
builder.Services.AddHostedService<SpeakingBoost.Services.Implementations.Background.GradingBackgroundService>();
builder.Services.AddScoped<IWebmToWavService, SpeakingBoost.Services.Implementations.Speaking.WebmToWavService>();
builder.Services.AddScoped<ITranscriptService, SpeakingBoost.Services.Implementations.Speaking.TranscriptService>();
builder.Services.AddScoped<ISpeechAnalyzeService, SpeakingBoost.Services.Implementations.Speaking.SpeechAnalyzeServiceHybrid>();
builder.Services.AddScoped<IEvaluateService, SpeakingBoost.Services.Implementations.Speaking.EvaluateService>();
builder.Services.AddScoped<IAnalyzeOrchestratorService, SpeakingBoost.Services.Implementations.Speaking.AnalyzeOrchestratorService>();
builder.Services.AddScoped<ISubmissionHandleService, SpeakingBoost.Services.Implementations.Speaking.SubmissionHandleService>();

// Student Repositories & Services
builder.Services.AddScoped<IStudentDashboardRepository, StudentDashboardRepository>();
builder.Services.AddScoped<IStudentDeadlineRepository, StudentDeadlineRepository>();
builder.Services.AddScoped<IPracticeRepository, PracticeRepository>();
builder.Services.AddScoped<IStudentSubmissionRepository, StudentSubmissionRepository>();

builder.Services.AddScoped<IStudentDashboardService, StudentDashboardService>();
builder.Services.AddScoped<IStudentDeadlineService, StudentDeadlineService>();
builder.Services.AddScoped<IPracticeService, PracticeService>();
builder.Services.AddScoped<IStudentSubmissionService, StudentSubmissionService>();

// Admin Repositories & Services
builder.Services.AddScoped<IAdminDashboardRepository, AdminDashboardRepository>();
builder.Services.AddScoped<IClassRepository, ClassRepository>();
builder.Services.AddScoped<IDeadlineRepository, DeadlineRepository>();
builder.Services.AddScoped<IExerciseRepository, ExerciseRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<IClassService, ClassService>();
builder.Services.AddScoped<IDeadlineService, DeadlineService>();
builder.Services.AddScoped<IExerciseService, ExerciseService>();
builder.Services.AddScoped<IStudentsAdminService, StudentsAdminService>();
builder.Services.AddScoped<IUserService, UserService>();

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
// app.UseDefaultFiles();
var defaultFilesOptions = new DefaultFilesOptions();
defaultFilesOptions.DefaultFileNames.Clear();
defaultFilesOptions.DefaultFileNames.Add("login.html");
app.UseDefaultFiles(defaultFilesOptions);
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
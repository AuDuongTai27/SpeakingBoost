using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeakingBoost.Models.DTOs;
using SpeakingBoost.Models.DTOs.Admin;
using SpeakingBoost.Models.EF;

// ================================================================
// DashboardController (Admin) — tương đương Admin/DashboardController (MVC)
//
// MVC cũ:                                    API mới:
// ─────────────────────────────────────────  ──────────────────────────────────────────
// GET /Admin/Dashboard/Index?classId=5      →  GET /api/admin/dashboard?classId=5
//
// Trả về:
//  - Danh sách lớp (cho dropdown)
//  - TotalStudents, TotalExercises, SubmissionRate, AverageOverallScore
//  - ProgressChartData (điểm TB theo từng bài tập)
//  - SkillsChartData   (TB 4 tiêu chí: phát âm, trôi chảy, từ vựng, ngữ pháp)
//  - RecentActivities  (5 bài nộp gần nhất)
// ================================================================

namespace SpeakingBoost.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/dashboard")]
    [Authorize(Roles = "teacher,superadmin")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ────────────────────────────────────────────────────────────
        // GET /api/admin/dashboard?classId=5
        // MVC cũ: Index(classId?) — tính toán số liệu, biểu đồ cho lớp đang chọn
        // ────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetDashboard([FromQuery] int? classId = null)
        {
            // 1. Lấy danh sách lớp (cho dropdown phía frontend)
            var classes = await _context.Classes
                .Include(c => c.Teacher)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new ClassDto
                {
                    ClassId     = c.ClassId,
                    ClassName   = c.ClassName,
                    TeacherId   = c.TeacherId,
                    TeacherName = c.Teacher != null ? c.Teacher.FullName : null,
                    CreatedAt   = c.CreatedAt
                })
                .ToListAsync();

            // Nếu không chọn lớp, dùng lớp mới nhất (giống MVC)
            if (classId == null && classes.Any())
                classId = classes.First().ClassId;

            var dto = new AdminDashboardDto
            {
                ClassList = classes
            };

            if (classId.HasValue)
            {
                // 2. Tải lớp + danh sách StudentId
                var selectedClass = await _context.Classes
                    .Include(c => c.StudentClasses)
                    .FirstOrDefaultAsync(c => c.ClassId == classId);

                if (selectedClass == null)
                    return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy lớp học."));

                var studentIds = selectedClass.StudentClasses.Select(sc => sc.StudentId).ToList();

                // 3. Lấy tất cả submissions của học sinh trong lớp
                var classSubmissions = await _context.Submissions
                    .Where(s => studentIds.Contains(s.StudentId))
                    .Include(s => s.Scores)
                    .Include(s => s.Student)
                    .Include(s => s.Exercise)
                    .ToListAsync();

                // 4. Tính số liệu (Cards)
                dto.TotalStudents = studentIds.Count;

                dto.TotalExercises = await _context.ClassExercises
                    .Where(ce => ce.ClassId == classId && ce.Deadline.HasValue)
                    .CountAsync();

                int totalExpected = dto.TotalStudents * dto.TotalExercises;
                dto.SubmissionRate = totalExpected > 0
                    ? (double)classSubmissions.Count / totalExpected * 100
                    : 0;

                var allScores = classSubmissions.SelectMany(s => s.Scores).ToList();
                dto.AverageOverallScore = allScores.Any()
                    ? allScores.Average(s => s.Overall ?? 0)
                    : 0;

                // 5. Biểu đồ tiến độ (điểm TB theo từng bài tập)
                dto.ProgressChartData = classSubmissions
                    .GroupBy(s => s.Exercise?.Title ?? "(Không có tiêu đề)")
                    .Select(g => new
                    {
                        Label  = g.Key,
                        Scores = g.SelectMany(s => s.Scores).Select(s => s.Overall ?? 0).ToList()
                    })
                    .Where(g => g.Scores.Any())
                    .Select(g => new ChartDataPointDto
                    {
                        Label = g.Label,
                        Value = g.Scores.Average()
                    })
                    .ToList();

                // 6. Biểu đồ kỹ năng (TB 4 tiêu chí)
                if (allScores.Any())
                {
                    dto.SkillsChartData = new List<ChartDataPointDto>
                    {
                        new() { Label = "Phát âm",  Value = allScores.Average(s => s.Pronunciation   ?? 0) },
                        new() { Label = "Trôi chảy", Value = allScores.Average(s => s.Grammar         ?? 0) },
                        new() { Label = "Từ vựng",  Value = allScores.Average(s => s.LexicalResource ?? 0) },
                        new() { Label = "Ngữ pháp", Value = allScores.Average(s => s.Coherence       ?? 0) }
                    };
                }

                // 7. Hoạt động gần đây (5 bài nộp mới nhất)
                dto.RecentActivities = classSubmissions
                    .OrderByDescending(s => s.CreatedAt)
                    .Take(5)
                    .Select(s => new RecentActivityDto
                    {
                        SubmissionId  = s.SubmissionId,
                        StudentName   = s.Student?.FullName ?? "",
                        ExerciseTitle = s.Exercise?.Title ?? "",
                        CreatedAt     = s.CreatedAt,
                        Overall       = s.Scores.OrderByDescending(sc => sc.CreatedAt).FirstOrDefault()?.Overall
                    })
                    .ToList();
            }

            return Ok(ApiResponse<AdminDashboardDto>.SuccessResponse(dto));
        }
    }
}

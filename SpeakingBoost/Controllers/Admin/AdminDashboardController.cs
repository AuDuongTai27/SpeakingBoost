using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeakingBoost.Models.DTOs;
using SpeakingBoost.Models.DTOs.Admin;
using SpeakingBoost.Models.EF;

namespace SpeakingBoost.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/dashboard")]
    [Authorize(Roles = "admin")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ────────────────────────────────────────────────────────────
        // GET /api/admin/dashboard?classId=5
        // Trả số liệu dashboard cho lớp được chọn
        // ────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetDashboard([FromQuery] int? classId = null)
        {
            // 1. Danh sách lớp (dropdown)
            var classes = await _context.Classes
                .Include(c => c.StudentClasses)
                .OrderBy(c => c.ClassName)
                .Select(c => new ClassDto
                {
                    ClassId      = c.ClassId,
                    ClassName    = c.ClassName,
                    StudentCount = c.StudentClasses.Count
                })
                .ToListAsync();

            // Dùng lớp đầu tiên nếu không chọn
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

                // 3. Submissions của lớp
                var classSubmissions = await _context.Submissions
                    .Where(s => studentIds.Contains(s.StudentId))
                    .Include(s => s.Scores)
                    .Include(s => s.Student)
                    .Include(s => s.Exercise)
                    .ToListAsync();

                // 4. Cards
                dto.TotalUsers = studentIds.Count;

                dto.TotalExercises = await _context.ClassExercises
                    .Where(ce => ce.ClassId == classId && ce.Deadline.HasValue)
                    .CountAsync();

                int totalExpected = dto.TotalUsers * dto.TotalExercises;
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
                        new() { Label = "Phát âm",   Value = allScores.Average(s => s.Pronunciation   ?? 0) },
                        new() { Label = "Ngữ pháp",  Value = allScores.Average(s => s.Grammar         ?? 0) },
                        new() { Label = "Từ vựng",   Value = allScores.Average(s => s.LexicalResource ?? 0) },
                        new() { Label = "Mạch lạc",  Value = allScores.Average(s => s.Coherence       ?? 0) }
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

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeakingBoost.Models.DTOs;
using SpeakingBoost.Models.DTOs.Student;
using SpeakingBoost.Models.EF;

namespace SpeakingBoost.Controllers.Student
{
    [ApiController]
    [Route("api/student/dashboard")]
    [Authorize(Roles = "user")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int? GetStudentId()
        {
            var claim = User.FindFirst("StudentId");
            return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboard()
        {
            var studentId = GetStudentId();
            if (studentId == null) return Unauthorized(ApiResponse<object>.ErrorResponse("Không tìm thấy thông tin người dùng."));

            var viewModel = new StudentDashboardDto();

            // 2. Lấy danh sách lớp
            var classIds = await _context.StudentClasses
                                         .Where(sc => sc.StudentId == studentId)
                                         .Select(sc => sc.ClassId)
                                         .ToListAsync();

            // 3. Lấy bài tập (Include Exercise để lấy TopicId, Part)
            var assignedExercises = await _context.ClassExercises
                                            .Include(ce => ce.Exercise)
                                            .Include(ce => ce.SchoolClass)
                                            .Where(ce => classIds.Contains(ce.ClassId))
                                            .ToListAsync();

            // 4. Lấy bài nộp
            var mySubmissions = await _context.Submissions
                                    .Include(s => s.Scores)
                                    .Where(s => s.StudentId == studentId)
                                    .ToListAsync();

            // --- XỬ LÝ DỮ LIỆU ---
            var assignmentsList = new List<StudentAssignmentDto>();
            int pendingCount = 0;
            int overdueCount = 0;

            foreach (var assignment in assignedExercises)
            {
                var sub = mySubmissions.FirstOrDefault(s => s.ExerciseId == assignment.ExerciseId);

                // MAPPING DỮ LIỆU SANG VIEWMODEL
                var vm = new StudentAssignmentDto
                {
                    ExerciseId = assignment.ExerciseId,
                    Title = assignment.Exercise.Title,
                    ClassName = assignment.SchoolClass.ClassName,
                    Deadline = assignment.Deadline,
                    Score = sub?.Scores?.FirstOrDefault()?.Overall,
                    TopicId = assignment.Exercise.TopicId ?? 0,
                    Part = 1
                };

                // Xác định trạng thái (Status)
                if (sub != null)
                {
                    vm.Status = "Submitted";
                }
                else
                {
                    if (assignment.Deadline.HasValue && assignment.Deadline.Value < DateTime.Now)
                    {
                        vm.Status = "Overdue";
                        overdueCount++;
                    }
                    else
                    {
                        vm.Status = "Pending";
                        pendingCount++;
                    }
                }
                assignmentsList.Add(vm);
            }

            // Sắp xếp & Lọc
            viewModel.UpcomingAssignments = assignmentsList
                .OrderBy(a => a.Status == "Submitted") // Chưa nộp lên đầu
                .ThenBy(a => a.Deadline)               // Deadline gần nhất lên đầu
                .Take(10)
                .ToList();

            // --- THỐNG KÊ ---
            viewModel.PendingAssignmentsCount = pendingCount;
            viewModel.OverdueAssignmentsCount = overdueCount;
            viewModel.CompletedExercisesCount = mySubmissions.Count;

            var allScores = mySubmissions.SelectMany(s => s.Scores).Select(sc => sc.Overall ?? 0).ToList();
            viewModel.AverageScore = allScores.Any() ? allScores.Average() : 0;

            // --- BIỂU ĐỒ ---
            var recentScores = mySubmissions
                .Where(s => s.Scores.Any())
                .OrderBy(s => s.CreatedAt)
                .TakeLast(10)
                .Select(s => new {
                    Date = s.CreatedAt.ToString("dd/MM"),
                    Score = s.Scores.First().Overall ?? 0
                })
                .ToList();

            viewModel.ChartLabels = recentScores.Select(x => x.Date).ToList();
            viewModel.ChartData = recentScores.Select(x => x.Score).ToList();

            return Ok(ApiResponse<StudentDashboardDto>.SuccessResponse(viewModel));
        }
    }
}

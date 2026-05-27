using SpeakingBoost.Helpers;
using SpeakingBoost.Models.DTOs.Student;
using SpeakingBoost.Repositories.Interfaces.Student;
using SpeakingBoost.Services.Interfaces.Student;

namespace SpeakingBoost.Services.Implementations.Student
{
    public class StudentDashboardService : IStudentDashboardService
    {
        private readonly IStudentDashboardRepository _repo;

        public StudentDashboardService(IStudentDashboardRepository repo)
        {
            _repo = repo;
        }

        public async Task<BaseResponse<StudentDashboardDto>> GetDashboardAsync(int studentId)
        {
            var classIds        = await _repo.GetClassIdsByStudentAsync(studentId);
            var assignedEx      = await _repo.GetAssignedExercisesAsync(classIds);
            var mySubmissions   = await _repo.GetStudentSubmissionsWithScoresAsync(studentId);

            var assignmentsList = new List<StudentAssignmentDto>();
            int pendingCount    = 0;
            int overdueCount    = 0;

            foreach (var assignment in assignedEx)
            {
                var sub = mySubmissions.FirstOrDefault(s => s.ExerciseId == assignment.ExerciseId);

                var vm = new StudentAssignmentDto
                {
                    ExerciseId = assignment.ExerciseId,
                    Title      = assignment.Exercise.Title,
                    ClassName  = assignment.SchoolClass.ClassName,
                    Deadline   = assignment.Deadline,
                    Score      = sub?.Scores?.FirstOrDefault()?.Overall,
                    TopicId    = assignment.Exercise.TopicId ?? 0,
                    Part       = 1
                };

                if (sub != null)
                {
                    vm.Status = "Submitted";
                }
                else if (assignment.Deadline.HasValue && assignment.Deadline.Value < DateTime.Now)
                {
                    vm.Status = "Overdue";
                    overdueCount++;
                }
                else
                {
                    vm.Status = "Pending";
                    pendingCount++;
                }

                assignmentsList.Add(vm);
            }

            var allScores   = mySubmissions.SelectMany(s => s.Scores).Select(sc => sc.Overall ?? 0).ToList();
            var recentScores = mySubmissions
                .Where(s => s.Scores.Any())
                .OrderBy(s => s.CreatedAt)
                .TakeLast(10)
                .Select(s => new
                {
                    Date  = s.CreatedAt.ToString("dd/MM"),
                    Score = s.Scores.First().Overall ?? 0
                }).ToList();

            var dto = new StudentDashboardDto
            {
                UpcomingAssignments       = assignmentsList
                    .OrderBy(a => a.Status == "Submitted")
                    .ThenBy(a => a.Deadline)
                    .Take(10)
                    .ToList(),
                PendingAssignmentsCount   = pendingCount,
                OverdueAssignmentsCount   = overdueCount,
                CompletedExercisesCount   = mySubmissions.Count,
                AverageScore              = allScores.Any() ? allScores.Average() : 0,
                ChartLabels               = recentScores.Select(x => x.Date).ToList(),
                ChartData                 = recentScores.Select(x => x.Score).ToList()
            };

            return BaseResponse<StudentDashboardDto>.Ok(dto);
        }
    }
}

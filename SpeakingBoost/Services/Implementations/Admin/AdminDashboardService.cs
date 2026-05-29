using SpeakingBoost.Models.DTOs.Admin;
using SpeakingBoost.Models.Entities;
using SpeakingBoost.Repositories.Interfaces.Admin;
using SpeakingBoost.Services.Interfaces.Admin;

namespace SpeakingBoost.Services.Implementations.Admin
{
    public class AdminDashboardService : IAdminDashboardService
    {
        private readonly IAdminDashboardRepository _dashboardRepository;

        public AdminDashboardService(IAdminDashboardRepository dashboardRepository)
        {
            _dashboardRepository = dashboardRepository;
        }

        public async Task<AdminDashboardDto> GetDashboardDataAsync(int? classId)
        {
            var classes = await _dashboardRepository.GetClassesSortedByNameAsync();

            var classList = classes.Select(c => new ClassDto
            {
                ClassId      = c.ClassId,
                ClassName    = c.ClassName,
                StudentCount = c.StudentClasses?.Count ?? 0
            }).ToList();

            if (classId == null && classList.Any())
            {
                classId = classList.First().ClassId;
            }

            var dto = new AdminDashboardDto
            {
                ClassList = classList
            };

            if (classId.HasValue)
            {
                var selectedClass = await _dashboardRepository.GetClassWithStudentClassesAsync(classId.Value);
                if (selectedClass == null)
                {
                    throw new KeyNotFoundException("Không tìm thấy lớp học.");
                }

                var studentIds = selectedClass.StudentClasses?.Select(sc => sc.StudentId).ToList() ?? new List<int>();

                dto.TotalUsers = studentIds.Count;
                dto.TotalExercises = await _dashboardRepository.CountClassExercisesWithDeadlinesAsync(classId.Value);

                if (studentIds.Any())
                {
                    var classSubmissions = await _dashboardRepository.GetSubmissionsByStudentIdsAsync(studentIds);

                    int totalExpected = dto.TotalUsers * dto.TotalExercises;
                    dto.SubmissionRate = totalExpected > 0
                        ? (double)classSubmissions.Count / totalExpected * 100
                        : 0;

                    var allScores = classSubmissions.SelectMany(s => s.Scores ?? new List<Score>()).ToList();
                    dto.AverageOverallScore = allScores.Any()
                        ? allScores.Average(s => s.Overall ?? 0)
                        : 0;

                    // Progress Chart Data (average overall score per exercise)
                    dto.ProgressChartData = classSubmissions
                        .GroupBy(s => s.Exercise?.Title ?? "(Không có tiêu đề)")
                        .Select(g => new
                        {
                            Label  = g.Key,
                            Scores = g.SelectMany(s => s.Scores ?? new List<Score>()).Select(s => s.Overall ?? 0).ToList()
                        })
                        .Where(g => g.Scores.Any())
                        .Select(g => new ChartDataPointDto
                        {
                            Label = g.Label,
                            Value = g.Scores.Average()
                        })
                        .ToList();

                    // Skills Chart Data (average of 4 criteria)
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

                    // Recent activities

                    //dto.RecentActivities = classSubmissions
                    //    .OrderByDescending(s => s.CreatedAt)
                    //    .Take(5)
                    //    .Select(s => new RecentActivityDto
                    //    {
                    //        SubmissionId  = s.SubmissionId,
                    //        StudentName   = s.Student?.FullName ?? "",
                    //        ExerciseTitle = s.Exercise?.Title ?? "",
                    //        CreatedAt     = s.CreatedAt,
                    //        Overall       = s.Scores?.OrderByDescending(sc => sc.CreatedAt).FirstOrDefault()?.Overall
                    //    })
                    //    .ToList();




                }
                else
                {
                    dto.SubmissionRate = 0;
                    dto.AverageOverallScore = 0;
                    dto.ProgressChartData = new List<ChartDataPointDto>();
                    dto.SkillsChartData = new List<ChartDataPointDto>();
                }
            }

            // Recent activities — latest submissions across the whole system
            var recentSubmissions = await _dashboardRepository.GetRecentSubmissionsAsync(5, 7);

            dto.RecentActivities = recentSubmissions
                .Select(s => new RecentActivityDto
                {
                    SubmissionId = s.SubmissionId,
                    StudentName = s.Student?.FullName ?? "",
                    ExerciseTitle = s.Exercise?.Title ?? "",
                    CreatedAt = s.CreatedAt,
                    Overall = s.Scores?.OrderByDescending(sc => sc.CreatedAt).FirstOrDefault()?.Overall
                })
                .ToList();

            return dto;
        }
    }
}

using SpeakingBoost.Models.DTOs.Admin;
using SpeakingBoost.Repositories.Interfaces.Admin;
using SpeakingBoost.Services.Interfaces.Admin;

namespace SpeakingBoost.Services.Implementations.Admin
{
    public class StudentsAdminService : IStudentsAdminService
    {
        private readonly IUserRepository _userRepository;
        private readonly IExerciseRepository _exerciseRepository;

        public StudentsAdminService(IUserRepository userRepository, IExerciseRepository exerciseRepository)
        {
            _userRepository = userRepository;
            _exerciseRepository = exerciseRepository;
        }

        public async Task<List<StudentSummaryDto>> GetStudentsSummaryAsync()
        {
            var students = await _userRepository.GetStudentsWithSubmissionsAndClassesAsync();
            var result = new List<StudentSummaryDto>();

            foreach (var student in students)
            {
                var classIds = student.StudentClasses?.Select(sc => sc.ClassId).ToList() ?? new List<int>();

                var assignedExercises = await _userRepository.GetClassExercisesWithDeadlinesAsync(classIds);

                int submitted = student.Submissions?
                    .Select(s => s.ExerciseId)
                    .Distinct()
                    .Count(exId => assignedExercises.Any(ae => ae.ExerciseId == exId)) ?? 0;

                int late = 0;
                if (student.Submissions != null)
                {
                    foreach (var sub in student.Submissions)
                    {
                        var assignment = assignedExercises.FirstOrDefault(ae => ae.ExerciseId == sub.ExerciseId);
                        if (assignment != null && sub.CreatedAt > assignment.Deadline)
                        {
                            late++;
                        }
                    }
                }

                int missing = assignedExercises
                    .Where(ae => ae.Deadline < DateTime.Now)
                    .Count(ae => student.Submissions == null || !student.Submissions.Any(s => s.ExerciseId == ae.ExerciseId));

                result.Add(new StudentSummaryDto
                {
                    StudentId          = student.UserId,
                    StudentName        = student.FullName,
                    Email              = student.Email,
                    SubmittedCount     = submitted,
                    SubmittedLateCount = late,
                    MissingCount       = missing
                });
            }

            return result;
        }

        public async Task<StudentDetailsDto> GetStudentDetailsAsync(int studentId)
        {
            var student = await _userRepository.GetStudentWithSubmissionsAndScoresAsync(studentId);
            if (student == null)
            {
                throw new KeyNotFoundException("Không tìm thấy học viên.");
            }

            var chartData = student.Submissions?
                .Where(s => s.Scores != null && s.Scores.Any())
                .OrderBy(s => s.CreatedAt)
                .Select(s => new
                {
                    Date  = s.CreatedAt.ToString("dd/MM"),
                    Score = s.Scores.OrderByDescending(sc => sc.CreatedAt).First().Overall ?? 0
                }).ToList() ?? new List<System.Tuple<string, double>>().Select(x => new { Date = "", Score = 0.0 }).ToList(); // dummy signature

            var dto = new StudentDetailsDto
            {
                UserId      = student.UserId,
                FullName    = student.FullName,
                Email       = student.Email,
                ChartLabels = chartData.Select(d => d.Date).ToList(),
                ChartValues = chartData.Select(d => d.Score).ToList(),
                Submissions = student.Submissions?
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => new SubmissionSummaryDto
                    {
                        SubmissionId  = s.SubmissionId,
                        ExerciseId    = s.ExerciseId,
                        ExerciseTitle = s.Exercise?.Title ?? "",
                        CreatedAt     = s.CreatedAt,
                        Overall       = s.Scores?.OrderByDescending(sc => sc.CreatedAt).FirstOrDefault()?.Overall,
                        Status        = s.Status.ToString()
                    }).ToList() ?? new List<SubmissionSummaryDto>()
            };

            return dto;
        }

        public async Task<object> GetAttemptHistoryAsync(int studentId, int exerciseId)
        {
            var student = await _userRepository.GetUserByIdAsync(studentId);
            var exercise = await _exerciseRepository.GetExerciseByIdAsync(exerciseId);

            if (student == null || exercise == null)
            {
                throw new KeyNotFoundException("Không tìm thấy học viên hoặc bài tập.");
            }

            var submissions = await _userRepository.GetSubmissionsWithScoresAsync(studentId, exerciseId);

            var history = submissions.Select(s =>
            {
                var latest = s.Scores?.OrderByDescending(sc => sc.CreatedAt).FirstOrDefault();
                return new AttemptHistoryAdminDto
                {
                    SubmissionId  = s.SubmissionId,
                    AttemptNumber = s.AttemptNumber,
                    CreatedAt     = s.CreatedAt,
                    Overall       = latest?.Overall,
                    Status        = s.Status.ToString(),
                    ErrorMessage  = s.ErrorMessage
                };
            }).ToList();

            return new
            {
                StudentName   = student.FullName,
                ExerciseTitle = exercise.Title,
                Items         = history
            };
        }

        public async Task<AttemptDetailAdminDto> GetSubmissionDetailAsync(int submissionId)
        {
            var submission = await _userRepository.GetSubmissionWithExerciseAndScoresAsync(submissionId);
            if (submission == null)
            {
                throw new KeyNotFoundException("Không tìm thấy bài nộp.");
            }

            var score = submission.Scores?.OrderByDescending(sc => sc.CreatedAt).FirstOrDefault();

            return new AttemptDetailAdminDto
            {
                SubmissionId    = submission.SubmissionId,
                StudentName     = submission.Student?.FullName ?? "",
                ExerciseTitle   = submission.Exercise?.Title ?? "",
                AudioPath       = submission.AudioPath,
                Transcript      = submission.Transcript,
                CreatedAt       = submission.CreatedAt,
                Overall         = score?.Overall,
                Pronunciation   = score?.Pronunciation,
                Grammar         = score?.Grammar,
                LexicalResource = score?.LexicalResource,
                Coherence       = score?.Coherence,
                AiFeedback      = score?.AiFeedback
            };
        }
    }
}

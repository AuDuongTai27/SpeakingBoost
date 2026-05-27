using SpeakingBoost.Helpers;
using SpeakingBoost.Models.DTOs.Student;
using SpeakingBoost.Models.Entities;
using SpeakingBoost.Repositories.Interfaces.Student;
using SpeakingBoost.Services.Interfaces.Student;

namespace SpeakingBoost.Services.Implementations.Student
{
    public class StudentDeadlineService : IStudentDeadlineService
    {
        private readonly IStudentDeadlineRepository _repo;

        public StudentDeadlineService(IStudentDeadlineRepository repo)
        {
            _repo = repo;
        }

        public async Task<BaseResponse<List<DeadlineExerciseDto>>> GetDeadlinesAsync(int studentId)
        {
            // Lấy classIds → deadlines → submissions
            var classIds             = await GetClassIdsByStudentAsync(studentId);
            var classExercises       = await _repo.GetDeadlinesByClassIdsAsync(classIds);
            var deadlineSubmissions  = await _repo.GetDeadlineSubmissionsAsync(studentId);

            var list = classExercises.Select(ce =>
            {
                var sub    = deadlineSubmissions.FirstOrDefault(s => s.ClassExerciseId == ce.ClassExerciseId);
                string status;
                if (sub != null)
                    status = "Submitted";
                else if (ce.Deadline.HasValue && ce.Deadline.Value < DateTime.Now)
                    status = "Overdue";
                else
                    status = "Pending";

                return new DeadlineExerciseDto
                {
                    ClassExerciseId = ce.ClassExerciseId,
                    ExerciseId      = ce.ExerciseId,
                    Title           = ce.Exercise.Title,
                    Question        = ce.Exercise.Question,
                    Type            = ce.Exercise.Type,
                    Deadline        = ce.Deadline,
                    ClassName       = ce.SchoolClass.ClassName,
                    Status          = status,
                    Score           = sub?.Scores?.FirstOrDefault()?.Overall,
                    SubmissionId    = sub?.SubmissionId ?? 0
                };
            }).ToList();

            return BaseResponse<List<DeadlineExerciseDto>>.Ok(list);
        }

        public async Task<BaseResponse<DeadlineQuestionDto>> GetDeadlineQuestionAsync(int classExerciseId, int studentId)
        {
            var ce = await _repo.GetClassExerciseWithDetailsAsync(classExerciseId);
            if (ce == null)
                return BaseResponse<DeadlineQuestionDto>.Fail("Không tìm thấy bài tập.", 404);

            var inClass = await _repo.IsStudentInClassAsync(studentId, ce.ClassId);
            if (!inClass)
                return BaseResponse<DeadlineQuestionDto>.Fail("Không có quyền truy cập.", 403);

            var attemptUsed = await _repo.CountAttemptsByClassExerciseAsync(studentId, classExerciseId);
            var latestSub   = await _repo.GetLatestDeadlineSubmissionAsync(studentId, classExerciseId);

            string status;
            if (latestSub != null)
                status = "Submitted";
            else if (ce.Deadline.HasValue && ce.Deadline.Value < DateTime.Now)
                status = "Overdue";
            else
                status = "Pending";

            int part = ce.Exercise.Type.ToLower() switch
            {
                "part1" => 1,
                "part2" => 2,
                "part3" => 3,
                _       => 1
            };

            var dto = new DeadlineQuestionDto
            {
                ClassExerciseId = ce.ClassExerciseId,
                ExerciseId      = ce.ExerciseId,
                Title           = ce.Exercise.Title,
                Question        = ce.Exercise.Question,
                Part            = part,
                Deadline        = ce.Deadline,
                ClassName       = ce.SchoolClass.ClassName,
                MaxAttempts     = ce.Exercise.MaxAttempts,
                AttemptUsed     = attemptUsed,
                Status          = status
            };

            return BaseResponse<DeadlineQuestionDto>.Ok(dto);
        }

        public async Task<BaseResponse<SubmitAudioResponse>> SubmitAudioAsync(
            IFormFile audio,
            int exerciseId,
            int classExerciseId,
            int part,
            int studentId,
            IServiceProvider serviceProvider)
        {
            var uploads  = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "audio");
            Directory.CreateDirectory(uploads);
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(audio.FileName)}";
            var filePath = Path.Combine(uploads, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await audio.CopyToAsync(stream);

            var audioPath  = $"/audio/{fileName}";
            var submission = new Submission
            {
                StudentId       = studentId,
                ExerciseId      = exerciseId,
                ClassExerciseId = classExerciseId,
                AudioPath       = audioPath,
                Status          = ProcessingStatus.Pending,
                CreatedAt       = DateTime.Now
            };

            var saved = await _repo.AddSubmissionAsync(submission);

            var queue  = serviceProvider.GetRequiredService<SpeakingBoost.Services.Background.BackgroundQueue>();
            var queued = queue.TryQueueBackgroundWorkItem(saved.SubmissionId);

            if (!queued)
            {
                saved.Status       = ProcessingStatus.Failed;
                saved.ErrorMessage = "Hệ thống đang bận. Vui lòng thử lại sau ít phút.";
                // Cập nhật trực tiếp qua context — repo không cần UpdateAsync riêng vì đã tracked
                await _repo.AddSubmissionAsync(saved); // SaveChanges sẽ update
                return BaseResponse<SubmitAudioResponse>.Fail("Hệ thống đang bận, vui lòng thử lại sau.", 429);
            }

            return BaseResponse<SubmitAudioResponse>.Ok(new SubmitAudioResponse
            {
                SubmissionId = saved.SubmissionId,
                Status       = "Pending",
                Message      = "Đang xử lý trong nền, vui lòng chờ..."
            });
        }

        // Helper nội bộ — lấy classIds
        private async Task<List<int>> GetClassIdsByStudentAsync(int studentId)
        {
            return await _repo.GetClassIdsByStudentAsync(studentId);
        }
    }
}

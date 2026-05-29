using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SpeakingBoost.Helpers;
using SpeakingBoost.Models.DTOs.Student;
using SpeakingBoost.Models.Entities;
using SpeakingBoost.Repositories.Interfaces.Student;
using SpeakingBoost.Services.Interfaces.Student;

namespace SpeakingBoost.Services.Implementations.Student
{
    public class PracticeService : IPracticeService
    {
        private readonly IPracticeRepository _repo;

        public PracticeService(IPracticeRepository repo)
        {
            _repo = repo;
        }

        public async Task<BaseResponse<List<PracticeTopicDto>>> GetTopicsAsync(int part)
        {
            var topicsWithCount = await _repo.GetTopicsWithCountAsync(part);
            var partKey = $"part{part}";

            var list = topicsWithCount.Select(t => new PracticeTopicDto
            {
                Id            = t.TopicId,
                Title         = t.Name,
                ForecastLabel = t.Description ?? "Bộ đề dự đoán",
                ForecastDate  = "2025",
                QuestionCount = t.QuestionCount
            }).ToList();

            return BaseResponse<List<PracticeTopicDto>>.Ok(list);
        }

        public async Task<BaseResponse<List<PracticeQuestionDto>>> GetTopicQuestionsAsync(int topicId, int part, int studentId)
        {
            var header = await _repo.GetTopicHeaderAsync(topicId);
            if (header == null)
            {
                return BaseResponse<List<PracticeQuestionDto>>.Fail("Không tìm thấy chủ đề.", 404);
            }

            var questionsWithAttempts = await _repo.GetTopicQuestionsWithAttemptsAsync(topicId, part, studentId);

            var list = questionsWithAttempts.Select(q => new PracticeQuestionDto
            {
                ExerciseId   = q.Exercise.ExerciseId,
                Title        = q.Exercise.Title,
                Question     = q.Exercise.Question,
                Type         = q.Exercise.Type,
                MaxAttempts  = q.Exercise.MaxAttempts,
                AttemptUsed  = q.AttemptUsed
            }).ToList();

            return BaseResponse<List<PracticeQuestionDto>>.Ok(list);
        }

        public async Task<BaseResponse<SubmitAudioResponse>> SubmitAudioAsync(
            IFormFile audio,
            int exerciseId,
            int part,
            int studentId,
            IServiceProvider serviceProvider)
        {
            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "audio");
            Directory.CreateDirectory(uploads);
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(audio.FileName)}";
            var filePath = Path.Combine(uploads, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await audio.CopyToAsync(stream);
            }

            var audioPath = $"/audio/{fileName}";

            var submission = new Submission
            {
                StudentId = studentId,
                ExerciseId = exerciseId,
                ClassExerciseId = null,
                AudioPath = audioPath,
                Status = ProcessingStatus.Pending,
                CreatedAt = DateTime.Now
            };

            var saved = await _repo.AddSubmissionAsync(submission);

            var queue = serviceProvider.GetRequiredService<SpeakingBoost.Services.Implementations.Background.BackgroundQueue>();
            var queued = queue.TryQueueBackgroundWorkItem(saved.SubmissionId);

            if (!queued)
            {
                saved.Status = ProcessingStatus.Failed;
                saved.ErrorMessage = "Hệ thống đang bận. Vui lòng thử lại sau ít phút.";
                await _repo.UpdateSubmissionAsync(saved);
                return BaseResponse<SubmitAudioResponse>.Fail("Hệ thống đang bận, vui lòng thử lại sau.", 429);
            }

            return BaseResponse<SubmitAudioResponse>.Ok(new SubmitAudioResponse
            {
                SubmissionId = saved.SubmissionId,
                Status = "Pending",
                Message = "Đang xử lý trong nền, vui lòng chờ..."
            });
        }
    }
}

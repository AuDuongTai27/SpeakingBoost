using System.Text.Json;
using SpeakingBoost.Helpers;
using SpeakingBoost.Models.DTOs.Student;
using SpeakingBoost.Models.Entities;
using SpeakingBoost.Repositories.Interfaces.Student;
using SpeakingBoost.Services.Interfaces.Student;

namespace SpeakingBoost.Services.Implementations.Student
{
    public class StudentSubmissionService : IStudentSubmissionService
    {
        private readonly IStudentSubmissionRepository _repo;

        public StudentSubmissionService(IStudentSubmissionRepository repo)
        {
            _repo = repo;
        }

        public async Task<BaseResponse<List<AttemptHistoryItemDto>>> GetAllHistoryAsync(int studentId)
        {
            var submissions = await _repo.GetAllByStudentAsync(studentId);

            var list = submissions.Select(s => new AttemptHistoryItemDto
            {
                SubmissionId = s.SubmissionId,
                ClassExerciseId = s.ClassExerciseId,
                ExerciseTitle = s.Exercise?.Title ?? "N/A",
                AttemptNumber = s.AttemptNumber,
                CreatedAt = s.CreatedAt,
                Overall = s.Scores.OrderByDescending(sc => sc.CreatedAt).FirstOrDefault()?.Overall,
                Status = s.Status.ToString(),
                ErrorMessage = s.ErrorMessage
            }).ToList();

            return BaseResponse<List<AttemptHistoryItemDto>>.Ok(list);
        }

        public async Task<BaseResponse<List<AttemptHistoryItemDto>>> GetPracticeHistoryAsync(int studentId, int exerciseId)
        {
            var submissions = await _repo.GetPracticeHistoryAsync(studentId, exerciseId);

            var list = submissions.Select(s => new AttemptHistoryItemDto
            {
                SubmissionId = s.SubmissionId,
                ClassExerciseId = s.ClassExerciseId,
                ExerciseTitle = s.Exercise?.Title ?? "N/A",
                AttemptNumber = s.AttemptNumber,
                CreatedAt = s.CreatedAt,
                Overall = s.Scores.OrderByDescending(sc => sc.CreatedAt).FirstOrDefault()?.Overall,
                Status = s.Status.ToString(),
                ErrorMessage = s.ErrorMessage
            }).ToList();

            return BaseResponse<List<AttemptHistoryItemDto>>.Ok(list);
        }

        public async Task<BaseResponse<List<AttemptHistoryItemDto>>> GetDeadlineHistoryAsync(int studentId, int classExerciseId)
        {
            var submissions = await _repo.GetDeadlineHistoryAsync(studentId, classExerciseId);

            var list = submissions.Select(s => new AttemptHistoryItemDto
            {
                SubmissionId = s.SubmissionId,
                ClassExerciseId = s.ClassExerciseId,
                ExerciseTitle = s.Exercise?.Title ?? "N/A",
                AttemptNumber = s.AttemptNumber,
                CreatedAt = s.CreatedAt,
                Overall = s.Scores.OrderByDescending(sc => sc.CreatedAt).FirstOrDefault()?.Overall,
                Status = s.Status.ToString(),
                ErrorMessage = s.ErrorMessage
            }).ToList();

            return BaseResponse<List<AttemptHistoryItemDto>>.Ok(list);
        }

        public async Task<BaseResponse<AttemptDetailDto>> GetAttemptDetailAsync(int submissionId, int studentId)
        {
            var submission = await _repo.GetDetailAsync(submissionId, studentId);
            if (submission == null)
            {
                return BaseResponse<AttemptDetailDto>.Fail("Không tìm thấy bài nộp.", 404);
            }

            var score = submission.Scores.OrderByDescending(sc => sc.CreatedAt).FirstOrDefault();

            object? fb = null;
            if (!string.IsNullOrWhiteSpace(score?.AiFeedback))
            {
                try
                {
                    fb = JsonSerializer.Deserialize<object>(score.AiFeedback);
                }
                catch { }
            }

            var dto = new AttemptDetailDto
            {
                SubmissionId = submission.SubmissionId,
                ClassExerciseId = submission.ClassExerciseId,
                ExerciseId = submission.ExerciseId,
                ExerciseTitle = submission.Exercise?.Title ?? "N/A",
                Type = submission.Exercise?.Type ?? "N/A",
                Question = submission.Exercise?.Question ?? "",
                SampleAnswer = submission.Exercise?.SampleAnswer,
                AudioPath = submission.AudioPath ?? "",
                CreatedAt = submission.CreatedAt,
                AttemptNumber = submission.AttemptNumber,
                OverallScore = score?.Overall,
                Pronunciation = score?.Pronunciation,
                Grammar = score?.Grammar,
                LexicalResource = score?.LexicalResource,
                Coherence = score?.Coherence,
                Transcript = submission.Transcript ?? "",
                AiFeedback = score?.AiFeedback,
                FeedbackJson = fb,
                ErrorMessage = submission.ErrorMessage,
                Status = submission.Status.ToString()
            };

            return BaseResponse<AttemptDetailDto>.Ok(dto);
        }

        public async Task<BaseResponse<object>> GetStatusAsync(int submissionId, int studentId)
        {
            var submission = await _repo.GetStatusAsync(submissionId, studentId);
            if (submission == null)
            {
                return BaseResponse<object>.Fail("Không tìm thấy bài nộp.", 404);
            }

            var score = submission.Scores.OrderByDescending(sc => sc.CreatedAt).FirstOrDefault();

            var data = new
            {
                Status = submission.Status.ToString(),
                Overall = score?.Overall,
                Pronunciation = score?.Pronunciation,
                Grammar = score?.Grammar,
                LexicalResource = score?.LexicalResource,
                Coherence = score?.Coherence,
                AiFeedback = score?.AiFeedback
            };

            return BaseResponse<object>.Ok(data);
        }
    }
}

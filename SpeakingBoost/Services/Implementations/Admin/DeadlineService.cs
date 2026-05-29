using SpeakingBoost.Models.DTOs.Admin;
using SpeakingBoost.Models.Entities;
using SpeakingBoost.Repositories.Interfaces.Admin;
using SpeakingBoost.Services.Interfaces.Email;
using SpeakingBoost.Services.Implementations.Email;
using SpeakingBoost.Services.Interfaces.Admin;

namespace SpeakingBoost.Services.Implementations.Admin
{
    public class DeadlineService : IDeadlineService
    {
        private readonly IDeadlineRepository _deadlineRepository;
        private readonly IEmailService _emailService;

        public DeadlineService(IDeadlineRepository deadlineRepository, IEmailService emailService)
        {
            _deadlineRepository = deadlineRepository;
            _emailService = emailService;
        }

        public async Task<object> GetActiveDeadlinesDataAsync()
        {
            var activeClassExercises = await _deadlineRepository.GetActiveDeadlinesAsync();
            var activeDeadlines = activeClassExercises.Select(ce => new ActiveDeadlineDto
            {
                ClassExerciseId = ce.ClassExerciseId,
                ClassId         = ce.ClassId,
                ClassName       = ce.SchoolClass?.ClassName ?? "",
                ExerciseId      = ce.ExerciseId,
                ExerciseTitle   = ce.Exercise?.Title ?? "",
                TopicName       = ce.Exercise?.VocabularyTopic?.Name,
                Deadline        = ce.Deadline
            }).ToList();

            var classes = await _deadlineRepository.GetClassesSortedAsync();
            var classDropdown = classes.Select(c => new { c.ClassId, c.ClassName }).ToList();

            var topics = await _deadlineRepository.GetTopicsSortedAsync();
            var topicDropdown = topics.Select(t => new
            {
                t.TopicId,
                t.Name,
                t.Description,
                ExerciseCount = t.Exercises?.Count ?? 0
            }).ToList();

            return new
            {
                ActiveDeadlines = activeDeadlines,
                Classes         = classDropdown,
                Topics          = topicDropdown
            };
        }

        public async Task<string> AssignTopicDeadlineAsync(AssignTopicDeadlineDto dto)
        {
            var exercises = await _deadlineRepository.GetExercisesByTopicIdAsync(dto.TopicId);
            if (!exercises.Any())
            {
                throw new InvalidOperationException("Chủ đề này chưa có câu hỏi nào.");
            }

            var schoolClass = await _deadlineRepository.GetClassByIdAsync(dto.ClassId);
            if (schoolClass == null)
            {
                throw new KeyNotFoundException("Không tìm thấy lớp học.");
            }

            var topic = await _deadlineRepository.GetTopicByIdAsync(dto.TopicId);
            if (topic == null)
            {
                throw new KeyNotFoundException("Không tìm thấy chủ đề.");
            }

            int added = 0, updated = 0;
            foreach (var exercise in exercises)
            {
                var existing = await _deadlineRepository.GetClassExerciseAsync(dto.ClassId, exercise.ExerciseId);
                if (existing != null)
                {
                    existing.Deadline = dto.Deadline;
                    await _deadlineRepository.UpdateClassExerciseAsync(existing);
                    updated++;
                }
                else
                {
                    await _deadlineRepository.AddClassExerciseAsync(new ClassExercise
                    {
                        ClassId    = dto.ClassId,
                        ExerciseId = exercise.ExerciseId,
                        Deadline   = dto.Deadline
                    });
                    added++;
                }
            }

            // Gửi email thông báo cho học viên trong lớp
            await SendTopicDeadlineNotification(dto.ClassId, topic.Name, schoolClass.ClassName, dto.Deadline, exercises.Count);

            string message = added > 0 && updated > 0
                ? $"Đã gán {added} câu hỏi mới và cập nhật {updated} câu hỏi của chủ đề '{topic.Name}' cho lớp {schoolClass.ClassName}!"
                : added > 0
                    ? $"Đã gán {added} câu hỏi của chủ đề '{topic.Name}' cho lớp {schoolClass.ClassName}!"
                    : $"Đã cập nhật deadline cho {updated} câu hỏi của chủ đề '{topic.Name}'.";

            return message;
        }

        public async Task DeleteDeadlineAsync(int id)
        {
            var assignment = await _deadlineRepository.GetClassExerciseByIdAsync(id);
            if (assignment == null)
            {
                throw new KeyNotFoundException("Không tìm thấy deadline để xóa.");
            }

            await _deadlineRepository.DeleteClassExerciseAsync(assignment);
        }

        public async Task DeleteTopicDeadlineFromClassAsync(int topicId, int classId)
        {
            var exerciseIds = (await _deadlineRepository.GetExercisesByTopicIdAsync(topicId))
                .Select(e => e.ExerciseId)
                .ToList();

            var assignments = await _deadlineRepository.GetClassExercisesAsync(classId, exerciseIds);
            if (!assignments.Any())
            {
                throw new KeyNotFoundException("Không tìm thấy deadline cần xóa.");
            }

            await _deadlineRepository.DeleteClassExercisesRangeAsync(assignments);
        }

        private async Task SendTopicDeadlineNotification(int classId, string topicName, string className, DateTime deadline, int exerciseCount)
        {
            var students = await _deadlineRepository.GetStudentsByClassIdAsync(classId);
            string subject = $"Bài tập mới - Chủ đề: {topicName} ({exerciseCount} câu hỏi)";

            foreach (var student in students)
            {
                try
                {
                    await _emailService.SendDeadlineNotification(
                        student.Email,
                        subject,
                        className,
                        deadline);
                }
                catch { /* Bỏ qua lỗi email từng học sinh */ }
            }
        }
    }
}

using Moq;
using SpeakingBoost.Models.Entities;
using SpeakingBoost.Repositories.Interfaces.Student;
using SpeakingBoost.Services.Implementations.Student;
using Xunit;

namespace SpeakingBoost.Tests.Services
{
    /// <summary>
    /// Unit Tests cho StudentSubmissionService
    /// Test phần logic xử lý dữ liệu submission KHÔNG cần audio thật
    ///
    /// LƯU Ý: SubmissionHandleService / EvaluateService / SpeechAnalyzeService
    /// KHÔNG được test ở đây vì yêu cầu audio thật và Azure AI API thật.
    /// </summary>
    public class SubmissionServiceTests
    {
        private readonly Mock<IStudentSubmissionRepository> _mockRepo;
        private readonly StudentSubmissionService           _service;

        public SubmissionServiceTests()
        {
            _mockRepo = new Mock<IStudentSubmissionRepository>();
            _service  = new StudentSubmissionService(_mockRepo.Object);
        }

        // ─────────────────────────────────────────────────────
        // TC-S01: GetAllHistoryAsync trả về đúng danh sách bài nộp của student
        // ─────────────────────────────────────────────────────
        [Fact]
        public async Task GetAllHistoryAsync_ValidStudentId_ReturnsSubmissionList()
        {
            // Arrange
            int studentId = 1;
            var fakeSubmissions = new List<Submission>
            {
                new Submission
                {
                    SubmissionId    = 1,
                    StudentId       = studentId,
                    ExerciseId      = 10,
                    AttemptNumber   = 1,
                    Status          = ProcessingStatus.Completed,
                    CreatedAt       = DateTime.Now.AddDays(-1),
                    Exercise        = new Exercise { Title = "Describe your hobby" },
                    Scores          = new List<Score>
                    {
                        new Score { Overall = 7.0, CreatedAt = DateTime.Now.AddDays(-1) }
                    }
                },
                new Submission
                {
                    SubmissionId    = 2,
                    StudentId       = studentId,
                    ExerciseId      = 11,
                    AttemptNumber   = 2,
                    Status          = ProcessingStatus.Completed,
                    CreatedAt       = DateTime.Now,
                    Exercise        = new Exercise { Title = "Talk about your family" },
                    Scores          = new List<Score>
                    {
                        new Score { Overall = 6.5, CreatedAt = DateTime.Now }
                    }
                }
            };

            _mockRepo.Setup(r => r.GetAllByStudentAsync(studentId)).ReturnsAsync(fakeSubmissions);

            // Act
            var result = await _service.GetAllHistoryAsync(studentId);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data!.Count);
            Assert.Equal("Describe your hobby",  result.Data[0].ExerciseTitle);
            Assert.Equal(7.0,                    result.Data[0].Overall);
            Assert.Equal("Completed",            result.Data[0].Status);
        }

        // ─────────────────────────────────────────────────────
        // TC-S02: GetAllHistoryAsync khi student chưa có bài nộp → trả về list rỗng
        // ─────────────────────────────────────────────────────
        [Fact]
        public async Task GetAllHistoryAsync_StudentWithNoSubmissions_ReturnsEmptyList()
        {
            // Arrange
            int studentId = 999;
            _mockRepo.Setup(r => r.GetAllByStudentAsync(studentId)).ReturnsAsync(new List<Submission>());

            // Act
            var result = await _service.GetAllHistoryAsync(studentId);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Empty(result.Data!);
        }

        // ─────────────────────────────────────────────────────
        // TC-S03: GetAttemptDetailAsync khi submissionId không thuộc về student → trả về Fail
        // ─────────────────────────────────────────────────────
        [Fact]
        public async Task GetAttemptDetailAsync_SubmissionNotBelongToStudent_ReturnsFail()
        {
            // Arrange — repo trả về null (submission không phải của student này)
            _mockRepo.Setup(r => r.GetDetailAsync(999, 1)).ReturnsAsync((Submission?)null);

            // Act
            var result = await _service.GetAttemptDetailAsync(999, 1);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.StatusCode);
        }

        // ─────────────────────────────────────────────────────
        // TC-S04: GetAttemptDetailAsync khi submission hợp lệ → trả về đầy đủ AttemptDetailDto
        // ─────────────────────────────────────────────────────
        [Fact]
        public async Task GetAttemptDetailAsync_ValidSubmission_ReturnsDetailDto()
        {
            // Arrange
            var submission = new Submission
            {
                SubmissionId  = 10,
                StudentId     = 1,
                ExerciseId    = 5,
                AttemptNumber = 1,
                Status        = ProcessingStatus.Completed,
                AudioPath     = "/audio/test.wav",
                Transcript    = "I am from Vietnam",
                CreatedAt     = DateTime.Now,
                Exercise = new Exercise
                {
                    Title        = "Hometown question",
                    Type         = "Part1",
                    Question     = "Where are you from?",
                    SampleAnswer = "I am from..."
                },
                Scores = new List<Score>
                {
                    new Score
                    {
                        Overall        = 7.5,
                        Pronunciation  = 7.0,
                        Grammar        = 7.5,
                        LexicalResource= 8.0,
                        Coherence      = 7.5,
                        AiFeedback     = "{\"feedback\": \"Good job\"}",
                        CreatedAt      = DateTime.Now
                    }
                }
            };

            _mockRepo.Setup(r => r.GetDetailAsync(10, 1)).ReturnsAsync(submission);

            // Act
            var result = await _service.GetAttemptDetailAsync(10, 1);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("Hometown question",  result.Data!.ExerciseTitle);
            Assert.Equal("I am from Vietnam",  result.Data.Transcript);
            Assert.Equal(7.5,                  result.Data.OverallScore);
            Assert.Equal("Part1",              result.Data.Type);
        }

        // ─────────────────────────────────────────────────────
        // TC-S05: GetStatusAsync khi submission không tồn tại → trả về Fail 404
        // ─────────────────────────────────────────────────────
        [Fact]
        public async Task GetStatusAsync_NonexistentSubmission_ReturnsFail404()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetStatusAsync(888, 1)).ReturnsAsync((Submission?)null);

            // Act
            var result = await _service.GetStatusAsync(888, 1);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.StatusCode);
        }
    }
}

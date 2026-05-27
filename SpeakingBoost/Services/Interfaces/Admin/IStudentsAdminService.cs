using SpeakingBoost.Models.DTOs.Admin;

namespace SpeakingBoost.Services.Interfaces.Admin
{
    public interface IStudentsAdminService
    {
        Task<List<StudentSummaryDto>> GetStudentsSummaryAsync();
        Task<StudentDetailsDto> GetStudentDetailsAsync(int studentId);
        Task<object> GetAttemptHistoryAsync(int studentId, int exerciseId);
        Task<AttemptDetailAdminDto> GetSubmissionDetailAsync(int submissionId);
    }
}

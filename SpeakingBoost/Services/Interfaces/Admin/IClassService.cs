using SpeakingBoost.Models.DTOs.Admin;

namespace SpeakingBoost.Services.Interfaces.Admin
{
    public interface IClassService
    {
        Task<List<ClassDto>> GetAllClassesAsync();
        Task<ClassDto> GetClassByIdAsync(int id);
        Task<ClassDto> CreateClassAsync(CreateClassDto dto);
        Task UpdateClassAsync(int id, UpdateClassDto dto);
        Task DeleteClassAsync(int id);
        Task<ClassDetailsDto> GetClassDetailsAsync(int id);
        Task AddStudentToClassAsync(int classId, AddStudentToClassDto dto);
        Task RemoveStudentFromClassAsync(int classId, int studentClassId);
        Task UpdateDeadlineAsync(int classExerciseId, UpdateDeadlineInClassDto dto);
    }
}

using SpeakingBoost.Models.Entities;

namespace SpeakingBoost.Repositories.Interfaces.Admin
{
    public interface IAdminDashboardRepository
    {
        Task<List<SchoolClass>> GetClassesSortedByNameAsync();
        Task<SchoolClass?> GetClassWithStudentClassesAsync(int classId);
        Task<List<Submission>> GetSubmissionsByStudentIdsAsync(List<int> studentIds);
        Task<int> CountClassExercisesWithDeadlinesAsync(int classId);
    }
}

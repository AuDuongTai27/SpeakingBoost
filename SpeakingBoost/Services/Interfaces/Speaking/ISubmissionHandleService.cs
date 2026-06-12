using System.Threading.Tasks;
using SpeakingBoost.Models.Entities;

namespace SpeakingBoost.Services.Interfaces.Speaking
{
    public interface ISubmissionHandleService
    {
        Task<(Submission submission, Score score)> ProcessAsync(
            int studentId,
            int exerciseId,
            string transcript,
            string audioPath,
            string aiJson,
            double? pronunciation = null);

        Task UpdateResultAsync(int submissionId, string transcript, string aiJson);
    }
}

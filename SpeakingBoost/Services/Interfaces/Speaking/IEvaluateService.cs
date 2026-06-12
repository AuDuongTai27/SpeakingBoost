using System.Threading.Tasks;

namespace SpeakingBoost.Services.Interfaces.Speaking
{
    public interface IEvaluateService
    {
        Task<string> EvaluateAsync(
            string transcript,
            string question,
            int part,
            int wordCount,
            double durationSec);
    }
}

using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace SpeakingBoost.Services.Interfaces.Speaking
{
    public interface ISpeechAnalyzeService
    {
        Task<string> AnalyzeAsync(
            IFormFile audioWav,
            string? referenceText,
            double threshold = 80.0,
            bool enableMiscue = true,
            bool enableProsody = false,
            bool filterFunctionWords = true,
            bool filterFillers = true,
            string lang = "en-US",
            bool includeRawJson = false);

        Task<string> AnalyzeFromWavPathAsync(
            string wavPath,
            string? referenceText,
            double threshold = 80.0,
            bool enableMiscue = true,
            bool enableProsody = false,
            bool filterFunctionWords = true,
            bool filterFillers = true,
            string lang = "en-US",
            bool includeRawJson = false);
    }
}

using SpeakingBoost.Models.DTOs.Student;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace SpeakingBoost.Services.Interfaces.Speaking
{
    public interface IAnalyzeOrchestratorService
    {
        Task<AnalyzeResult> ProcessFileAsync(string filePath, string question, int part);
        Task<AnalyzeResult> ProcessAsync(IFormFile audio, string question, int part);
    }
}

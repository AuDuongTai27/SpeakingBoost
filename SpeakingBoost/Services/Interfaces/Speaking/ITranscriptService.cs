using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace SpeakingBoost.Services.Interfaces.Speaking
{
    public interface ITranscriptService
    {
        Task<string> TranscribeAsync(IFormFile audio);
    }
}

using System.Threading.Tasks;

namespace SpeakingBoost.Services.Interfaces.Speaking
{
    public interface IWebmToWavService
    {
        Task<string> ConvertAsync(string inputPath);
    }
}

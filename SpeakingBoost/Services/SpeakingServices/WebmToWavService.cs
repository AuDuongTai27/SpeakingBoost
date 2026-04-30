using NAudio.Wave;

namespace SpeakingBoost.Services.SpeakingServices
{
    public class WebmToWavService
    {
        public class NoiseGateProvider : ISampleProvider
        {
            private readonly ISampleProvider source;
            private readonly float threshold; // Ngu?ng (0.0 d?n 1.0). Du?i m?c này s? b? t?t ti?ng.

            public NoiseGateProvider(ISampleProvider source, float threshold = 0.02f)
            {
                this.source = source;
                this.threshold = threshold;
            }

            public WaveFormat WaveFormat => source.WaveFormat;

            public int Read(float[] buffer, int offset, int count)
            {
                int samplesRead = source.Read(buffer, offset, count);

                for (int i = 0; i < samplesRead; i++)
                {
                    // Ki?m tra biên d? âm thanh (tuy?t d?i)
                    if (Math.Abs(buffer[offset + i]) < threshold)
                    {
                        buffer[offset + i] = 0.0f; // Mute (t?t h?n ti?ng ?n n?n)
                    }
                }
                return samplesRead;
            }
        }
        public async Task<string> ConvertAsync(string inputPath)
        {
            if (!File.Exists(inputPath))
                throw new FileNotFoundException($"? Không tìm th?y file ngu?n: {inputPath}");

            var outputPath = Path.ChangeExtension(inputPath, ".wav");

            try
            {
                Console.WriteLine("?? [Convert] Ðang convert WebM ? WAV PCM 16-bit...");

                using (var reader = new MediaFoundationReader(inputPath))
                {
                    // Bu?c 1: Resample v? chu?n Azure (16kHz, Mono)
                    // Luu ý: Chua ép v? 16-bit ? dây v?i, d? m?c d?nh IEEE Float d? x? lý Noise Gate t?t hon
                    var targetFormat = WaveFormat.CreateIeeeFloatWaveFormat(16000, 1);

                    using (var resampler = new MediaFoundationResampler(reader, targetFormat))
                    {
                        resampler.ResamplerQuality = 60;

                        // --- B?T Ð?U X? LÝ NOISE GATE ---

                        // 1. Chuy?n d?i sang ISampleProvider d? x? lý s? li?u (Float)
                        var sampleProvider = resampler.ToSampleProvider();

                        // 2. Áp d?ng Noise Gate (Ngu?ng 0.05f là ví d?, b?n có th? ch?nh nh? hon n?u b? m?t ti?ng nói)
                        var gateProvider = new NoiseGateProvider(sampleProvider, threshold: 0.03f);

                        // 3. Chuy?n ngu?c v? PCM 16-bit d? dúng chu?n file WAV d?u ra
                        var waveProvider16 = gateProvider.ToWaveProvider16();

                        // --- K?T THÚC X? LÝ ---

                        // Ghi ra file
                        WaveFileWriter.CreateWaveFile(outputPath, waveProvider16);
                    }
                }

                Console.WriteLine($"? [Convert & Denoise Done] {outputPath}");
                return outputPath;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"? L?i convert WebM ? WAV: {ex.Message}", ex);
            }
        }

    }
}

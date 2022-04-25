using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace FunciSox
{
    public class ManualTests
    {
        //[FunctionName(nameof(RunMp3ToWav))]
        //public static async Task<IActionResult> RunMp3ToWav(
        //    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "TestRunMp3ToWav")] HttpRequest req,
        //    ILogger log)
        //{
        //    string mp3 = Environment.GetEnvironmentVariable("TestMp3File");
        //    if (string.IsNullOrEmpty(mp3))
        //    {
        //        throw new InvalidOperationException($"Missing environment variable 'TestMp3File'.");
        //    }

        //    string wav = Path.Combine(
        //        Path.GetDirectoryName(mp3), 
        //        $"{Path.GetFileNameWithoutExtension(mp3)}.wav"
        //    );

        //    log.LogWarning($"Running ConvertMp3ToWav source='{mp3}' target='{wav}'");

        //    await Toolbox.ConvertMp3ToWav(mp3, wav, log);

        //    return new OkResult();
        //}

        [FunctionName(nameof(RunAudioTools))]
        public static async Task<IActionResult> RunAudioTools(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "TestRunAudioTools")] HttpRequest req,
            ILogger log)
        {
            string mp3 = Environment.GetEnvironmentVariable("TestMp3File");
            if (string.IsNullOrEmpty(mp3))
            {
                throw new InvalidOperationException($"Missing environment variable 'TestMp3File'.");
            }

            string wav = Path.Combine(
                Path.GetDirectoryName(mp3),
                $"{Path.GetFileNameWithoutExtension(mp3)}.wav"
            );

            string wavFast = Path.Combine(
                Path.GetDirectoryName(mp3),
                $"{Path.GetFileNameWithoutExtension(mp3)}-faster.wav"
            );

            string mp3Out = Path.Combine(
                Path.GetDirectoryName(mp3),
                $"{Path.GetFileNameWithoutExtension(mp3)}-out.mp3"
            );

            log.LogWarning($"Running ConvertMp3ToWav source='{mp3}' target='{wav}'");

            await Toolbox.ConvertMp3ToWav(mp3, wav, log);

            string tempo = "1.1";  // 10% faster.

            log.LogWarning($"Running MakeFasterWav source='{wav}' target='{wavFast}' tempo='{tempo}'");

            await Toolbox.MakeFasterWav(wav, wavFast, tempo, log);

            log.LogWarning($"Running EncodeWavToMp3 source='{wav}' target='{mp3Out}'");

            await Toolbox.EncodeWavToMp3(wav, mp3Out, log);

            // TODO: Get ID3 tags when doing initial conversion of MP3 to WAV.
            //log.LogWarning($"Running CopyID3Tags source='{mp3}' target='{mp3Out}'");
            //await Toolbox.CopyID3Tags(mp3, mp3Out, log);

            return new OkResult();
        }

    }
}

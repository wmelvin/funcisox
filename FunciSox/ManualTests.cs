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
        [FunctionName(nameof(RunMp3ToWav))]
        public static async Task<IActionResult> RunMp3ToWav(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "RunMp3ToWav")] HttpRequest req,
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

            log.LogWarning($"Running ConvertMp3ToWav source='{mp3}' target='{wav}'");

            await Toolbox.ConvertMp3ToWav(mp3, wav, log);

            return new OkResult();
        }
    }
}

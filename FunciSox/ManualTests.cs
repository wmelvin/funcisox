using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace FunciSox
{
    public class ManualTests
    {

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

            log.LogWarning("Running ConvertMp3ToWav source='{mp3}' target='{wav}'", mp3, wav);

            await Toolbox.ConvertMp3ToWav(mp3, wav, "", log);

            string tempo = "1.1";  // 10% faster.

            log.LogWarning(
                "Running MakeFasterWav source='{wav}' target='{wavFast}' tempo='{tempo}'", 
                wav, wavFast, tempo);

            await Toolbox.MakeFasterWav(wav, wavFast, tempo, "", log);

            log.LogWarning("Running EncodeWavToMp3 source='{wav}' target='{mp3Out}'", wav, mp3Out);

            var tags = new TagAttr()
            {
                Album = "TestAlbum",
                Artist = "TestArtist",
                Title = "TestTitle"
            };

            await Toolbox.EncodeWavToMp3(wav, mp3Out, tags, log);

            log.LogWarning("RunAudioTools done.");

            return new OkResult();
        }


        [FunctionName(nameof(TestRunId3))]
        public static async Task<IActionResult> TestRunId3(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "TestRunId3")] HttpRequest req,
            ILogger log)
        {
            string mp3 = Environment.GetEnvironmentVariable("TestMp3File");
            if (string.IsNullOrEmpty(mp3))
            {
                throw new InvalidOperationException($"Missing environment variable 'TestMp3File'.");
            }

            log.LogWarning("Running GetId3Tags source='{mp3}'", mp3);

            TagAttr tags = await Toolbox.GetId3Tags(mp3, "", log);

            log.LogWarning($"Tags: {tags.Artist}, {tags.Album}, {tags.Title}, {tags.TrackNum}, {tags.Year}, {tags.Comment}");

            return new OkResult();
        }

    }
}

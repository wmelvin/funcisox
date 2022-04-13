using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Mail;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FunciSox
{
    public static class ActivityFunctions
    {

        [FunctionName(nameof(GetFasterWavTempos))]
        public static string[] GetFasterWavTempos([ActivityTrigger] object input)
        {
            return Environment.GetEnvironmentVariable("WavFasterTempos").Split(",").ToArray();
        }


        [FunctionName(nameof(ConvertToWav))]
        public static async Task<string> ConvertToWav([ActivityTrigger]
            string inputMp3, ILogger log)
        {
            log.LogInformation($"Converting {inputMp3}.");

            string outFileName = $"{Path.GetFileNameWithoutExtension(inputMp3)}.wav";

            // TODO: Run SoX convert here.
            await Task.Delay(5000);

            return outFileName;
        }


        [FunctionName(nameof(ProcessWav))]
        public static async Task<string> ProcessWav([ActivityTrigger]
            string inputWav, ILogger log)
        {
            log.LogInformation($"Processing {inputWav}.");

            string outFileName = $"{Path.GetFileNameWithoutExtension(inputWav)}-proc.wav";

            // TODO: Run SoX effects here.
            await Task.Delay(5000);

            return outFileName;
        }

        [FunctionName(nameof(FasterWav))]
        public static async Task<WavProcessAttr> FasterWav([ActivityTrigger]
            WavProcessAttr wavAttr, ILogger log)
        {
            log.LogInformation($"Processing change tempo to {wavAttr.Tempo}: {wavAttr.FilePath}");

            string outFileName = $"{Path.GetFileNameWithoutExtension(wavAttr.FilePath)}-faster-{wavAttr.Version}.wav";

            // TODO: Run SoX effects here.
            await Task.Delay(5000);

            return new WavProcessAttr
            {
                FilePath = outFileName,
                Tempo = wavAttr.Tempo,
                Version = wavAttr.Version
            };
        }

        [FunctionName(nameof(ConvertToMp3))]
        public static async Task<string> ConvertToMp3([ActivityTrigger]
            string inputWav, ILogger log)
        {
            log.LogInformation($"Converting {inputWav} to mp3.");

            string outFileName = $"{Path.GetFileNameWithoutExtension(inputWav)}.mp3";

            // TODO: Run Lame here.
            await Task.Delay(5000);

            return outFileName;
        }


        [FunctionName(nameof(SendDownloadAvailableEmail))]
        public static void SendDownloadAvailableEmail(
            [ActivityTrigger] string[] mp3Files,
            [SendGrid(ApiKey = "SendGridKey")] out SendGridMessage message,
            [Table("Downloads", "AzureWebJobsStorage")] out Download download,
            ILogger log)
        {
            var downloadCode = Guid.NewGuid().ToString("N");
            download = new Download
            {
                PartitionKey = "Download",
                RowKey = downloadCode,
                OrchestrationId = null
            };
            // TODO: Pass in orchestration ID.

            message = null;
            // TODO: Finish building email message.

            //log.LogInformation($"Sending download email.");
            //foreach (var mp3 in mp3Files)
            //{
            //    log.LogInformation($"File: {mp3}");
            //}

        }


        [FunctionName(nameof(Cleanup))]
        public static async Task<string> Cleanup([ActivityTrigger]
            string[] fileNames, ILogger log)
        {
            foreach(var file in fileNames.Where(f => f != null))
            {
                log.LogInformation($"Cleanup: Delete {file}");

                // TODO: Delete file here.
                await Task.Delay(1000);
            }
            return "Cleanup finished.";
        }
    }
}

using Azure.Storage.Blobs;
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


        [FunctionName(nameof(GetDownloadTimeSpan))]
        public static TimeSpan GetDownloadTimeSpan(
            [ActivityTrigger] object input, ILogger log)
        {
            // The "DownloadTimeout" setting is string formatted as numeric
            // interval followed by a single character to indicate the
            // TimeSpan unit. The unit can be M (minutes), H (hours) or D (days).
            // If any other unit, or no unit, is given then the interval is seconds.
            // Examples: "30M", "24H", "3D".

            var setting = Environment.GetEnvironmentVariable("DownloadTimeout");
            if (!string.IsNullOrEmpty(setting))
            {
                string value = setting[0..^1];
                string unit = setting[^1..];
                if (int.TryParse(value, out int n))
                {
                    return unit.ToUpper() switch
                    {
                        "M" => TimeSpan.FromMinutes(n),
                        "H" => TimeSpan.FromHours(n),
                        "D" => TimeSpan.FromDays(n),
                        _ => TimeSpan.FromSeconds(n),
                    };
                }
            }
            log.LogWarning("DownloadTimeout setting not valid. Default is 20 seconds.");
            return TimeSpan.FromSeconds(20);
        }


        [FunctionName(nameof(ConvertToWav))]
        public static async Task<WavProcessAttr> ConvertToWav(
            [ActivityTrigger] string inputMp3, 
            [Blob("funcisox")] BlobContainerClient client,
            ILogger log)
        {
            var outBlobName = $"{Path.GetFileNameWithoutExtension(inputMp3)}-{Guid.NewGuid():N}.wav";
            var outBlob = client.GetBlobClient(outBlobName);
            log.LogInformation($"Converting {inputMp3}.");

            return await Helpers.ConvertToWavAndUpload(inputMp3, outBlob, log);
        }


        [FunctionName(nameof(FasterWav))]
        public static async Task<WavFasterAttr> FasterWav(
            [ActivityTrigger] WavProcessAttr wavAttr,
            [Blob("funcisox")] BlobContainerClient client,
            ILogger log)
        {
            log.LogInformation($"Processing change tempo to {wavAttr.Tempo}: {wavAttr.FileLocation}");

            string suffix = $"-faster-{wavAttr.Version}";

            string outBlobName = $"{wavAttr.FileNameStem}-{Guid.NewGuid():N}{suffix}.wav";

            var outBlob = client.GetBlobClient(outBlobName);

            string localWavIn = "";

            try
            {
                localWavIn = await Helpers.DownloadLocalAsync(wavAttr.FileLocation);
                
                var localAttr = new WavProcessAttr
                {
                    FileLocation = localWavIn,
                    FileNameStem = wavAttr.FileNameStem,
                    Tempo = wavAttr.Tempo,
                    Version = wavAttr.Version
                };

                //return await Helpers.UploadFasterWav(localAttr, outBlob, log);

                var blobLocation = await Helpers.UploadFasterWav(localAttr, outBlob, log);
                return new WavFasterAttr()
                {
                    FileLocation = blobLocation,
                    FileNamePrefix = localAttr.FileNameStem,
                    FileNameSuffix = suffix
                };
            }
            finally
            {
                Helpers.DeleteTempFiles(log, localWavIn);
            }
        }


        [FunctionName(nameof(ConvertToMp3))]
        public static async Task<string> ConvertToMp3(
            [ActivityTrigger] Mp3ProcessAttr mp3Attr,
            [Blob("funcisox")] BlobContainerClient client,
            ILogger log)
        {
            log.LogInformation($"Converting {mp3Attr.WavLocation} to mp3.");

            string outBlobName = $"{mp3Attr.FileNamePrefix}{mp3Attr.FileNameSuffix}-{Guid.NewGuid():N}.mp3";

            var outBlob = client.GetBlobClient(outBlobName);

            string localWavIn = "";

            try
            {
                localWavIn = await Helpers.DownloadLocalAsync(mp3Attr.WavLocation);

                var localAttr = new Mp3ProcessAttr
                {
                    WavLocation = localWavIn,
                    FileNamePrefix = mp3Attr.FileNamePrefix,
                    FileNameSuffix = mp3Attr.FileNameSuffix,
                    Id3Tags = mp3Attr.Id3Tags,
                    LocalCopyPath = mp3Attr.LocalCopyPath
                };

                return await Helpers.UploadMp3(localAttr, outBlob, log);
            }
            finally
            {
                Helpers.DeleteTempFiles(log, localWavIn);
            }
        }

        [FunctionName(nameof(SendDownloadAvailableEmail))]
        public static void SendDownloadAvailableEmail(
            [ActivityTrigger] DownloadAttr downloadAttr,
            [SendGrid(ApiKey = "SendGridKey")] out SendGridMessage message,
            [Table("Downloads", "AzureWebJobsStorage")] out Download download,
            ILogger log)
        {
            var downloadCode = Guid.NewGuid().ToString("N");
            download = new Download
            {
                PartitionKey = "Download",
                RowKey = downloadCode,
                OrchestrationId = downloadAttr.OrchestrationId
            };
            var recipientAddress = new EmailAddress(Environment.GetEnvironmentVariable("EmailRecipientAddress"));
            var senderAddress = new EmailAddress(Environment.GetEnvironmentVariable("EmailSenderAddress"));
            var host = Environment.GetEnvironmentVariable("Host");
            var funcAddr = $"{host}/api/AcknowledgeDownload/{downloadCode}";
            var recdLink = funcAddr + "?result=Downloaded";

            // TODO: Add links for each file.

            var body = $"Downloads available: ..download links here...<br>"
                + $"<a href=\"{recdLink}\">Acknowledge files are downloaded</a>";
            message = new SendGridMessage();
            message.Subject = "Files to download";
            message.From = senderAddress;
            message.AddTo(recipientAddress);
            message.HtmlContent = body;

            log.LogWarning(body);

            // TODO: Actually send email message.

        }


        [FunctionName(nameof(Cleanup))]
        public static async Task<string> Cleanup([ActivityTrigger]
            string[] fileNames,
            [Blob("funcisox")] BlobContainerClient client,
            ILogger log)
        {
            foreach(var uri in fileNames.Where(f => f != null))
            {
                var file = Path.GetFileName(new Uri(uri).LocalPath);
                log.LogInformation($"Cleanup: Delete {file}");
                try
                {
                    await client.DeleteBlobAsync(file);
                }
                catch (Exception e)
                {
                    log.LogError($"Cannot delete blob '{file}'", e);
                }
            }
            return "Cleanup finished.";
        }
    }
}

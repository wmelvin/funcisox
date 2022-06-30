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

        //[FunctionName(nameof(CopyToolFiles))]
        //public static int CopyToolFiles([ActivityTrigger] object input, ILogger log)
        //{
        //    // 2022-06-29: This is a kludge. I have not found the correct
        //    // MSBUILD settings for FunciSox.csproj that will put the
        //    // Tools\*.exe and Tools\*.dll files into the same directory
        //    // on publishing. It always wants to put DLLs under bin\Tools.
        //    //
        //    // TODO: Learn how to get rid of this kludge.

        //    int nFiles = 0;

        //    string toolsPath = Toolbox.GetToolsPath();

        //    log.LogInformation($"CopyToolFiles: toolsPath='{toolsPath}'");

        //    string[] files = new string[] { "libmad.dll.tmp", "cyggomp-1.dll.tmp", "cygwin1.dll.tmp" };

        //    foreach (string f in files)
        //    {
        //        string src = Path.Combine(toolsPath, f);
        //        string dst = Path.Combine(toolsPath, Path.GetFileNameWithoutExtension(f));

        //        if (File.Exists(src))
        //        {
        //            if (File.Exists(dst))
        //            {
        //                log.LogInformation($"CopyToolFiles: '{dst}' already exists.");
        //            }
        //            else
        //            {
        //                log.LogInformation($"CopyToolFiles: '{src}' to '{dst}'");
        //                File.Copy(src, dst);
        //                nFiles++;
        //            }
        //        }
        //        else
        //        {
        //            log.LogInformation($"CopyToolFiles: Cannot find '{src}'");
        //        }
        //    }
        //    return nFiles;
        //}

        //[FunctionName(nameof(CopyToolFiles))]
        //public static int CopyToolFiles([ActivityTrigger] object input, ILogger log)
        //{
        //    // 2022-06-29: This is a kludge. I have not found the correct
        //    // MSBUILD settings for FunciSox.csproj that will put the
        //    // Tools\*.exe and Tools\*.dll files into the same directory
        //    // on publishing. It always wants to put DLLs under bin\Tools.
        //    //
        //    // TODO: Learn how to get rid of this kludge.

        //    int nFiles = 0;

        //    string toolsPath = Toolbox.GetToolsPath();

        //    string binToolsPath = Path.Combine(Directory.GetParent(toolsPath).FullName, "bin", "Tools");

        //    log.LogInformation($"CopyToolFiles: toolsPath='{toolsPath}'");
        //    log.LogInformation($"CopyToolFiles: binToolsPath='{binToolsPath}'");

        //    string[] files = new string[] { "libmad.dll", "cyggomp-1.dll", "cygwin1.dll" };

        //    foreach (string f in files)
        //    {
        //        string src = Path.Combine(toolsPath, f);
        //        string dst = Path.Combine(binToolsPath, f);

        //        if (File.Exists(src))
        //        {
        //            if (File.Exists(dst))
        //            {
        //                log.LogInformation($"CopyToolFiles: '{dst}' already exists.");
        //            }
        //            else
        //            {
        //                log.LogInformation($"CopyToolFiles: '{src}' to '{dst}'");
        //                File.Copy(src, dst);
        //                nFiles++;
        //            }
        //        }
        //        else
        //        {
        //            log.LogInformation($"CopyToolFiles: Cannot find '{src}'");
        //        }
        //    }
        //    return nFiles;
        //}


        [FunctionName(nameof(GetEnvSettings))]
        public static SettingsAttr GetEnvSettings([ActivityTrigger] object input, ILogger log)
        {

            // The "DownloadTimeout" setting is string formatted as numeric
            // interval followed by a single character to indicate the
            // TimeSpan unit. The unit can be M (minutes), H (hours) or D (days).
            // If any other unit, or no unit, is given then the interval is seconds.
            // Examples: "30M", "24H", "3D".

            TimeSpan ts = TimeSpan.FromSeconds(20);   // default

            var setting = Environment.GetEnvironmentVariable("DownloadTimeout");
            if (!string.IsNullOrEmpty(setting))
            {
                string value = setting[0..^1];
                string unit = setting[^1..];
                if (int.TryParse(value, out int n))
                {
                    ts = unit.ToUpper() switch
                    {
                        "M" => TimeSpan.FromMinutes(n),
                        "H" => TimeSpan.FromHours(n),
                        "D" => TimeSpan.FromDays(n),
                        _ => TimeSpan.FromSeconds(n),
                    };
                }
            }

            log.LogWarning("DownloadTimeout setting not valid. Default is 20 seconds.");

            var localCopyPath = Environment.GetEnvironmentVariable("LocalCopyPath");
            
            return new SettingsAttr()
            {
                DownloadTimeout = ts,
                LocalCopyPath = localCopyPath ?? ""
            };
        }


        [FunctionName(nameof(GetFasterWavTempos))]
        public static string[] GetFasterWavTempos([ActivityTrigger] object input)
        {
            return Environment.GetEnvironmentVariable("WavFasterTempos").Split(",").ToArray();
        }


        [FunctionName(nameof(ConvertToWav))]
        public static async Task<WavProcessAttr> ConvertToWav(
            [ActivityTrigger] string inputMp3, 
            [Blob(ContainerNames.Work)] BlobContainerClient client,
            ILogger log)
        {
            var outBlobName = $"{Path.GetFileNameWithoutExtension(inputMp3)}-{Guid.NewGuid():N}.wav";
            var outBlob = client.GetBlobClient(outBlobName);
            string mp3Name = Path.GetFileNameWithoutExtension(new Uri(inputMp3).LocalPath);
            string localMp3 = "";

            log.LogInformation($"ConvertToWav: '{inputMp3}' to '{outBlobName}'");

            try
            {
                localMp3 = await Helpers.DownloadLocalAsync(inputMp3, log);
                return await Helpers.ConvertToWavAndUpload(localMp3, mp3Name, outBlob, log);
            }
            finally
            {
                Helpers.DeleteTempFiles(log, localMp3);
            }
        }


        [FunctionName(nameof(FasterWav))]
        public static async Task<WavFasterAttr> FasterWav(
            [ActivityTrigger] WavProcessAttr wavAttr,
            [Blob(ContainerNames.Work)] BlobContainerClient client,
            ILogger log)
        {
            log.LogInformation($"Processing change tempo to {wavAttr.Tempo}: {wavAttr.FileLocation}");

            string suffix = $"-faster-{wavAttr.Version}";

            string outBlobName = $"{wavAttr.FileNameStem}-{Guid.NewGuid():N}{suffix}.wav";

            var outBlob = client.GetBlobClient(outBlobName);

            string localWavIn = "";

            try
            {
                localWavIn = await Helpers.DownloadLocalAsync(wavAttr.FileLocation, log);
                
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
            [Blob(ContainerNames.Output)] BlobContainerClient client,
            ILogger log)
        {
            log.LogInformation($"Converting {mp3Attr.WavLocation} to mp3.");

            string outBlobName = $"{mp3Attr.FileNamePrefix}{mp3Attr.FileNameSuffix}-{Guid.NewGuid():N}.mp3";

            var outBlob = client.GetBlobClient(outBlobName);

            string localWavIn = "";

            try
            {
                localWavIn = await Helpers.DownloadLocalAsync(mp3Attr.WavLocation, log);

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
            [ActivityTrigger] DownloadsAvailableAttr downloadAttr,
            [SendGrid(ApiKey = "SendGridKey")] out SendGridMessage message,
            [Table("Downloads", "AzureWebJobsStorage")] out DownloadTableAttr download,
            ILogger log)
        {
            // (1)

            var downloadCode = Guid.NewGuid().ToString("N");
            download = new DownloadTableAttr
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


        [FunctionName(nameof(CleanupWork))]
        public static async Task<string> CleanupWork([ActivityTrigger]
            string[] fileNames,
            [Blob(ContainerNames.Work)] BlobContainerClient client,
            ILogger log)
        {
            foreach (var uri in fileNames.Where(f => f != null))
            {
                var file = Path.GetFileName(new Uri(uri).LocalPath);
                log.LogInformation($"CleanupWork: Delete {file}");
                try
                {
                    await client.DeleteBlobAsync(file);
                }
                catch (Exception e)
                {
                    log.LogError($"CleanupWork: Cannot delete blob '{file}'", e);
                }
            }
            return "CleanupWork finished.";
        }

        [FunctionName(nameof(CleanupOutput))]
        public static async Task<string> CleanupOutput([ActivityTrigger]
            string[] fileNames,
            [Blob(ContainerNames.Output)] BlobContainerClient client,
            ILogger log)
        {
            foreach (var uri in fileNames.Where(f => f != null))
            {
                var file = Path.GetFileName(new Uri(uri).LocalPath);
                log.LogInformation($"CleanupOutput: Delete {file}");
                try
                {
                    await client.DeleteBlobAsync(file);
                }
                catch (Exception e)
                {
                    log.LogError($"CleanupOutput: Cannot delete blob '{file}'", e);
                }
            }
            return "CleanupOutput finished.";
        }
    }
}


// (1) TableAttribute Class
// https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.webjobs.tableattribute


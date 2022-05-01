using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

namespace FunciSox
{
    public class OrchestratorFunctions
    {
        [FunctionName(nameof(AudioProcessOrchestrator))]

        // TODO: Implement custom return class (custom DTO) for Task<>?

        public static async Task<object> AudioProcessOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            log = context.CreateReplaySafeLogger(log);

            var mp3InLocation = context.GetInput<string>();

            WavProcessAttr normalWav = null;
            WavFasterAttr[] fasterWavs = null;
            string[] mp3Results = null;
            var downloadResult = "Unknown";
            var filesForCleanup = new List<string>();

            log.LogInformation("BEGIN AudioProcessOrchestrator");

            try
            {
                SettingsAttr settings = await context.CallActivityAsync<SettingsAttr>(
                    "GetEnvSettings", null);

                normalWav = await context.CallActivityAsync<WavProcessAttr>(
                    "ConvertToWav", mp3InLocation);

                filesForCleanup.Add(normalWav.FileLocation);

                fasterWavs = await context.CallSubOrchestratorAsync<WavFasterAttr[]>(
                    nameof(FasterWavOrchestrator), new WavProcessAttr {
                        FileLocation = normalWav.FileLocation,
                        FileNameStem = normalWav.FileNameStem,
                        Tempo = null,
                        Version = 0
                    }) ;

                // Add normal-speed WAV to a new list.                
                var wavs = new List<Mp3ProcessAttr> { new Mp3ProcessAttr(){
                    WavLocation = normalWav.FileLocation,
                    FileNamePrefix = normalWav.FileNameStem,
                    FileNameSuffix = "",
                    Id3Tags = normalWav.Id3Tags
                } };

                // Append faster WAVs to the list.                
                foreach (var fw in fasterWavs)
                {
                    wavs.Add(new Mp3ProcessAttr()
                    {
                        WavLocation = fw.FileLocation,
                        FileNamePrefix = fw.FileNamePrefix,
                        FileNameSuffix = fw.FileNameSuffix,
                        Id3Tags = normalWav.Id3Tags
                    });

                    filesForCleanup.Add(fw.FileLocation);
                }

                // Convert the WAVs to MP3s.

                var mp3Tasks = new List<Task<string>>();

                foreach (var wav in wavs)
                {
                    var task = context.CallActivityAsync<string>("ConvertToMp3", new Mp3ProcessAttr() { 
                        WavLocation = wav.WavLocation,
                        FileNamePrefix = wav.FileNamePrefix,
                        FileNameSuffix = wav.FileNameSuffix,
                        Id3Tags = normalWav.Id3Tags,
                        LocalCopyPath = settings.LocalCopyPath
                    });

                    mp3Tasks.Add(task);
                }

                mp3Results = await Task.WhenAll(mp3Tasks);

                foreach (var mp3 in mp3Results)
                {
                    filesForCleanup.Add(mp3);
                }
                                
                await context.CallActivityAsync("SendDownloadAvailableEmail", new DownloadAttr()
                {
                    OrchestrationId = context.InstanceId,
                    Mp3Files = mp3Results
                });

                log.LogInformation($"Download timeout is {settings.DownloadTimeout}");

                try
                {
                    downloadResult = await context.WaitForExternalEvent<string>(
                        "DownloadResult", settings.DownloadTimeout);
                }
                catch (TimeoutException)
                {
                    downloadResult = "Timeout";
                }

                log.LogInformation($"Download Result: '{downloadResult}'. Starting Cleanup.");

                await context.CallActivityAsync<string>("Cleanup", filesForCleanup);

            }
            catch (Exception e)
            {
                log.LogError($"Exception in activity: {e.Message}");

                await context.CallActivityAsync<string>("Cleanup", filesForCleanup);

                return new
                {
                    Error = "Process failed.",
                    Message = e.Message
                };
            }

            log.LogInformation("END AudioProcessOrchestrator");

            return new
            {
                WavIn = normalWav,
                Mp3Out = mp3Results,
                Downloaded = downloadResult
            };

        }

        [FunctionName(nameof(FasterWavOrchestrator))]
        public static async Task<WavFasterAttr[]> FasterWavOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var wavOutAttr = context.GetInput<WavProcessAttr>();

            var fasterTempos = await context.CallActivityAsync<string[]>(
                "GetFasterWavTempos", null);

            var tempoTasks = new List<Task<WavFasterAttr>>();

            int version = 0;
            foreach (var tempo in fasterTempos)
            {
                version += 1;
                var attr = new WavProcessAttr()
                {
                    FileLocation = wavOutAttr.FileLocation,
                    FileNameStem = wavOutAttr.FileNameStem,
                    Tempo = tempo,
                    Version = version
                };
                var task = context.CallActivityAsync<WavFasterAttr>("FasterWav", attr);
                tempoTasks.Add(task);
            }
            var tempoResults = await Task.WhenAll(tempoTasks);

            return tempoResults;
        }

    }
}

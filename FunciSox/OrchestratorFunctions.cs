﻿using System;
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

            log.LogInformation("BEGIN AudioProcessOrchestrator");

            var mp3InLocation = context.GetInput<string>();

            log.LogInformation($"AudioProcessOrchestrator: mp3InLocation = '{mp3InLocation}'");

            WavProcessAttr normalWav = null;
            WavFasterAttr[] fasterWavs = null;
            string[] mp3Results = null;
            var downloadResult = "Unknown";
            var dirtyWork = new List<string>();
            var dirtyOutput = new List<string>();


            try
            {
                SettingsAttr settings = await context.CallActivityAsync<SettingsAttr>(
                    "GetEnvSettings", null);

                log.LogInformation($"AudioProcessOrchestrator: Call 'ConvertToWav'");

                normalWav = await context.CallActivityAsync<WavProcessAttr>(
                    "ConvertToWav", mp3InLocation);

                dirtyWork.Add(normalWav.FileLocation);

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

                    dirtyWork.Add(fw.FileLocation);
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
                    dirtyOutput.Add(mp3);
                }
                                
                await context.CallActivityAsync("SendDownloadAvailableEmail", new DownloadsAvailableAttr()
                {
                    OrchestrationId = context.InstanceId,
                    Mp3Files = mp3Results
                });

                await context.CallActivityAsync<string>("CleanupWork", dirtyWork);

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

                await context.CallActivityAsync<string>("CleanupOutput", dirtyOutput);

            }
            catch (Exception e)
            {
                log.LogError($"Exception in activity: {e.Message}");

                await context.CallActivityAsync<string>("CleanupWork", dirtyWork);
                await context.CallActivityAsync<string>("CleanupOutput", dirtyOutput);

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


// Microsoft.Azure.WebJobs Namespace
// https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.webjobs

// StartOrchestrationArgs.FunctionName Property
// https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.webjobs.startorchestrationargs.functionname

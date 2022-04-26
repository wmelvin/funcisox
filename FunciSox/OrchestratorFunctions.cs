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
            [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            log = context.CreateReplaySafeLogger(log);

            var mp3InLocation = context.GetInput<string>();

            //string wavInLocation = null;
            WavProcessAttr wavInLocation = null;

            string[] fasterWavs = null;
            string[] mp3Results = null;
            var downloadResult = "Unknown";
            var files = new List<string>();

            log.LogInformation("BEGIN AudioProcessOrchestrator");

            try
            {
                TimeSpan timeout = await context.CallActivityAsync<TimeSpan>(
                    "GetDownloadTimeSpan", null);

                wavInLocation = await context.CallActivityAsync<WavProcessAttr>(
                    "ConvertToWav", mp3InLocation);

                files.Add(wavInLocation.FileLocation);

                fasterWavs = await context.CallSubOrchestratorAsync<string[]>(
                    nameof(FasterWavOrchestrator), new WavProcessAttr {
                        FileLocation = wavInLocation.FileLocation,
                        FileNameStem = Path.GetFileNameWithoutExtension(mp3InLocation),
                        Tempo = null,
                        Version = 0
                    }) ;

                // Add normal-speed WAV to new list.
                var wavs = new List<string> { wavInLocation.FileLocation };

                // Append the faster WAVs.
                foreach (var uri in fasterWavs)
                {
                    wavs.Add(uri);
                    files.Add(uri);
                }

                // Convert the WAVs to MP3s.

                var mp3Tasks = new List<Task<string>>();

                foreach (var wav in wavs)
                {
                    var task = context.CallActivityAsync<string>("ConvertToMp3", wav);
                    mp3Tasks.Add(task);
                }

                mp3Results = await Task.WhenAll(mp3Tasks);

                foreach (var mp3 in mp3Results)
                {
                    files.Add(mp3);
                }

                await context.CallActivityAsync("SendDownloadAvailableEmail", new DownloadAttr()
                {
                    OrchestrationId = context.InstanceId,
                    Mp3Files = mp3Results
                });

                log.LogInformation($"Download timeout is {timeout}");

                try
                {
                    downloadResult = await context.WaitForExternalEvent<string>("DownloadResult", timeout);
                }
                catch (TimeoutException)
                {
                    downloadResult = "Timeout";
                }

                log.LogInformation($"Download Result: '{downloadResult}'. Starting Cleanup.");

                await context.CallActivityAsync<string>("Cleanup", files);

            }
            catch (Exception e)
            {
                log.LogError($"Exception in activity: {e.Message}");

                await context.CallActivityAsync<string>("Cleanup", files);

                return new
                {
                    Error = "Process failed.",
                    Message = e.Message
                };
            }

            log.LogInformation("END AudioProcessOrchestrator");

            return new
            {
                WavIn = wavInLocation,
                Mp3Out = mp3Results,
                Downloaded = downloadResult
            };

        }

        [FunctionName(nameof(FasterWavOrchestrator))]
        public static async Task<string[]> FasterWavOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var wavOutAttr = context.GetInput<WavProcessAttr>();

            var fasterTempos = await context.CallActivityAsync<string[]>(
                "GetFasterWavTempos", null);

            var tempoTasks = new List<Task<string>>();

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
                var task = context.CallActivityAsync<string>("FasterWav", attr);
                tempoTasks.Add(task);
            }
            var tempoResults = await Task.WhenAll(tempoTasks);

            return tempoResults;
        }

    }
}

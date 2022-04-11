using System;
using System.Collections.Generic;
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

            string wavInLocation = null;
            string wavOutLocation = null;
            WavProcessAttr[] fasterWavs = null;
            string[] mp3Results = null;

            log.LogInformation("BEGIN AudioProcessOrchestrator");

            try
            {
                wavInLocation = await context.CallActivityAsync<string>(
                    "ConvertToWav", mp3InLocation);

                wavOutLocation = await context.CallActivityAsync<string>(
                    "ProcessWav", wavInLocation);

                fasterWavs = await context.CallSubOrchestratorAsync<WavProcessAttr[]>(
                    nameof(FasterWavOrchestrator), wavOutLocation);

                // Add normal-speed WAV to new list.
                var wavs = new List<string>
                {
                    wavOutLocation
                };

                // Append the faster WAVs.
                foreach (var attr in fasterWavs)
                {
                    wavs.Add(attr.FilePath);
                }

                // Convert the WAVs to MP3s.

                var mp3Tasks = new List<Task<string>>();

                foreach (var wav in wavs)
                {
                    var task = context.CallActivityAsync<string>("ConvertToMp3", wav);
                    mp3Tasks.Add(task);
                }
                mp3Results = await Task.WhenAll(mp3Tasks);

            }
            catch (Exception e)
            {
                log.LogError($"Exception in activity: {e.Message}");

                var files = new List<string>
                {
                    mp3InLocation, wavInLocation, wavOutLocation
                };
                foreach (var a in fasterWavs)
                {
                    files.Add(a.FilePath);
                }
                foreach (var s in mp3Results)
                {
                    files.Add(s);
                }

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
                wavIn = wavInLocation,
                wavOut = wavOutLocation,
                mp3Out = mp3Results
            };

        }

        [FunctionName(nameof(FasterWavOrchestrator))]
        public static async Task<WavProcessAttr[]> FasterWavOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var wavOutLocation = context.GetInput<string>();

            var fasterTempos = await context.CallActivityAsync<string[]>(
                "GetFasterWavTempos", null);

            var tempoTasks = new List<Task<WavProcessAttr>>();

            int version = 0;
            foreach (var tempo in fasterTempos)
            {
                version += 1;
                var attr = new WavProcessAttr()
                {
                    FilePath = wavOutLocation,
                    Tempo = tempo,
                    Version = version
                };
                var task = context.CallActivityAsync<WavProcessAttr>("FasterWav", attr);
                tempoTasks.Add(task);
            }
            var tempoResults = await Task.WhenAll(tempoTasks);

            return tempoResults;
        }

    }
}


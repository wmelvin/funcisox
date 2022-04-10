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
            WavProcessAttr[] tempoResults = null;
            string[] mp3Results = null;

            try
            {
                wavInLocation = await context.CallActivityAsync<string>(
                    "ConvertToWav", mp3InLocation);

                wavOutLocation = await context.CallActivityAsync<string>(
                    "ProcessWav", wavInLocation);

                var fasterTempos = new[] { "1.09", "1.18" };
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
                tempoResults = await Task.WhenAll(tempoTasks);

                // Add normal-speed WAV to new list.
                var wavs = new List<string>
                {
                    wavOutLocation
                };

                // Append the faster WAVs.
                foreach (var attr in tempoResults)
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
                foreach (var a in tempoResults)
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

            return new
            {
                wavIn = wavInLocation,
                wavOut = wavOutLocation,
                mp3Out = mp3Results
            };

        }

    }
}


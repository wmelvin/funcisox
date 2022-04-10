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
            string mp3OutLocation = null;
            WavProcessAttr wavFasterAttr1 = null;
            WavProcessAttr wavFasterAttr2 = null;

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
                var tempoResults = await Task.WhenAll(tempoTasks);

                mp3OutLocation = await context.CallActivityAsync<string>(
                    "ConvertToMp3", wavOutLocation);

                // TODO: Convert the faster WAVs to MP3s.
            }
            catch (Exception e)
            {
                log.LogError($"Exception in activity: {e.Message}");

                await context.CallActivityAsync<string>(
                    "Cleanup", new[] { mp3InLocation, wavInLocation, wavOutLocation, mp3OutLocation }
                    );

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
                mp3Out = mp3OutLocation
            };

        }

    }
}


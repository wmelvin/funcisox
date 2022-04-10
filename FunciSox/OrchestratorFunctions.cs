using System;
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

                wavFasterAttr1 = await context.CallActivityAsync<WavProcessAttr>(
                    "FasterWav", new WavProcessAttr
                    {
                        FilePath = wavOutLocation,
                        Tempo = "1.09",
                        Version = 1
                    });

                wavFasterAttr2 = await context.CallActivityAsync<WavProcessAttr>(
                    "FasterWav", new WavProcessAttr
                    {
                        FilePath = wavOutLocation,
                        Tempo = "1.18",
                        Version = 2
                    });

                mp3OutLocation = await context.CallActivityAsync<string>(
                    "ConvertToMp3", wavOutLocation);
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


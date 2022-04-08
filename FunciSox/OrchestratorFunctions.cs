using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace FunciSox
{
    public class OrchestratorFunctions
    {
        [FunctionName(nameof(AudioProcessOrchestrator))]

        // TODO: Implement custom return class (custom DTO) for Task<>?

        public static async Task<object> AudioProcessOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var mp3InLocation = context.GetInput<string>();

            var wavInLocation = await context.CallActivityAsync<string>(
                "ConvertToWav", mp3InLocation);

            var wavOutLocation = await context.CallActivityAsync<string>(
                "ProcessWav", wavInLocation);

            //var wavFasterLocation = await context.CallActivityAsync<string>(
            //    "UpTempoWav", wavOutLocation);

            var mp3OutLocation = await context.CallActivityAsync<string>(
                "ConvertToMp3", wavOutLocation);

            return new
            {
                wavIn = wavInLocation,
                wavOut = wavOutLocation,
                mp3Out = mp3OutLocation
            };

        }

    }
}


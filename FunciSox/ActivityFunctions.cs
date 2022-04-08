using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace FunciSox
{
    public static class ActivityFunctions
    {

        [FunctionName(nameof(ConvertToWav))]
        public static async Task<string> ConvertToWav([ActivityTrigger]
            string inputMp3, ILogger log)
        {
            log.LogInformation($"Converting {inputMp3}.");

            string outFileName = $"{Path.GetFileNameWithoutExtension(inputMp3)}.wav";

            // TODO: Run SoX convert here.
            await Task.Delay(5000);

            return outFileName;
        }


        [FunctionName(nameof(ProcessWav))]
        public static async Task<string> ProcessWav([ActivityTrigger]
            string inputWav, ILogger log)
        {
            log.LogInformation($"Processing {inputWav}.");

            string outFileName = $"{Path.GetFileNameWithoutExtension(inputWav)}-proc.wav";

            // TODO: Run SoX effects here.
            await Task.Delay(5000);

            return outFileName;
        }

        [FunctionName(nameof(ConvertToMp3))]
        public static async Task<string> ConvertToMp3([ActivityTrigger]
            string inputWav, ILogger log)
        {
            log.LogInformation($"Converting {inputWav} to mp3.");

            string outFileName = $"{Path.GetFileNameWithoutExtension(inputWav)}.mp3";

            // TODO: Run Lame here.
            await Task.Delay(5000);

            return outFileName;
        }

    }
}

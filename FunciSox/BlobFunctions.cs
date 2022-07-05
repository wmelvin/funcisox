using Azure.Storage.Blobs;
using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace FunciSox
{
    public class BlobFunctions
    {
        [FunctionName(nameof(ProcessUploadedFile))]
        public static async Task ProcessUploadedFile(
            [BlobTrigger(ContainerNames.InputPath)] BlobClient blob,
            string name,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            var sas = blob.GetReadSAS(TimeSpan.FromHours(2));
            var orcId = await starter.StartNewAsync("AudioProcessOrchestrator", null, sas);
            
            log.LogInformation(
                "FunciSox/ProcessUploadedFile: Started orchestration ({orcId}) for file {blobName}", 
                orcId, blob.Name);
        }
    }
}

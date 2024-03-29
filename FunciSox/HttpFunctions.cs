﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace FunciSox
{
    public static class HttpFunctions
    {
        [FunctionName(nameof(HttpStartAudioProcess))]
        public static async Task<IActionResult> HttpStartAudioProcess(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            string mp3 = req.GetQueryParameterDictionary()["mp3"];
            if (mp3 == null)
            {
                return new BadRequestObjectResult(
                    "No mp3 file location specified.");
            }

            string instanceId = await starter.StartNewAsync(
                "AudioProcessOrchestrator", null, mp3);

            log.LogInformation("FunciSox/HttpStartAudioProcess: Started orchestration with ID = '{instanceId}'.", instanceId);

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName(nameof(AcknowledgeDownload))]
        public static async Task<IActionResult> AcknowledgeDownload(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "AcknowledgeDownload/{id}")]
            HttpRequest req,
            [DurableClient] IDurableOrchestrationClient client,
            [Table("Downloads", "Download", "{id}", Connection = "AzureWebJobsStorage")] DownloadsAvailableAttr downloadAttr,
            ILogger log)
        {
            string result = req.GetQueryParameterDictionary()["result"];

            log.LogWarning(
                "Sending download acknowledgement to {OrchestrationId} (result={result}).", 
                downloadAttr.OrchestrationId, result );

            await client.RaiseEventAsync(downloadAttr.OrchestrationId, "DownloadResult", result);

            return new OkResult();
        }

        [FunctionName(nameof(GetAppProperties))]
        public static IActionResult GetAppProperties(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GetAppProperties")]
            HttpRequest req,
            ILogger log)
        {
            string result = $"\"AppProperties.AppVersion\": \"{AppProperties.AppVersion}\"";

            log.LogWarning($"FunciSox/GetAppProperties: {result}");

            return new OkObjectResult(result);
        }

    }
}

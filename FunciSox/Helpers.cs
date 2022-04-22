using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
//using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunciSox
{
    static class Helpers
    {
        private static string GetTempWorkFolder()
        {
            var workFolder = Path.Combine(
                Path.GetTempPath(), 
                "work", 
                $"{DateTime.Today:yyyy-MM-dd}");
            Directory.CreateDirectory(workFolder);
            return workFolder;
        }

        //public static string GetReadSAS(this ICloudBlob blob, TimeSpan validFor)
        public static string GetReadSAS(this BlobClient blob, TimeSpan validFor)
        {
            //var sas = blob.GetSharedAccessSignature(new SharedAccessBlobPolicy()
            //{
            //    Permissions = SharedAccessBlobPermissions.Read,
            //    SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
            //    SharedAccessExpiryTime = DateTimeOffset.UtcNow + validFor
            //});
            //var location = blob.StorageUri.PrimaryUri.AbsoluteUri + sas;
            var sas = blob.GenerateSasUri(Azure.Storage.Sas.BlobSasPermissions.Read, DateTimeOffset.UtcNow + validFor);
            var location = blob.Uri.AbsoluteUri + sas;
            return location;
        }

        public static async Task<string> ConvertToWavAndUpload(
            string mp3Path, BlobClient outBlob, ILogger log)
        {
            var outFilePath = Path.Combine(GetTempWorkFolder(), $"{Guid.NewGuid()}.wav");
            try
            {
                await Toolbox.ConvertMp3ToWav(mp3Path, outFilePath, log);
                await outBlob.UploadAsync(outFilePath);
            }
            finally
            {
                DeleteTempFiles(log, outFilePath);
            }
            // TODO: Replace fixed 1 hour duration?
            return GetReadSAS(outBlob, TimeSpan.FromHours(1));
        }

        public static void DeleteTempFiles(ILogger log, params string[] files)
        {
            foreach (var file in files)
            {
                try
                {
                    if (!string.IsNullOrEmpty(file) && File.Exists(file))
                    {
                        File.Delete(file);
                    }
                }
                catch (Exception e)
                {
                    log.LogError($"Cannot delete temp file '{file}'", e);
                }
            }
        }
    }
}

﻿using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
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

            return sas.ToString();
        }

        public static async Task<WavProcessAttr> ConvertToWavAndUpload(
            string mp3Path, BlobClient outBlob, ILogger log)
        {
            var wavRawPath = Path.Combine(GetTempWorkFolder(), $"temp-{Guid.NewGuid():N}.wav");
            var wavProcPath = Path.Combine(
                Path.GetDirectoryName(wavRawPath), 
                $"{Path.GetFileNameWithoutExtension(wavRawPath)}-proc.wav");
            
            TagAttr tags = null;
            
            try
            {
                // Seems like there is no point converting to WAV and
                // then processing the WAV in separate activities, so
                // do them both here.
                await Toolbox.ConvertMp3ToWav(mp3Path, wavRawPath, log);
                await Toolbox.ProcessWav(wavRawPath, wavProcPath, log);

                tags = await Toolbox.GetId3Tags(mp3Path, log);

               await outBlob.UploadAsync(wavProcPath);
            }
            finally
            {
                DeleteTempFiles(log, wavRawPath, wavProcPath);
            }

            // TODO: Replace fixed 1 hour duration?
            var readSas = GetReadSAS(outBlob, TimeSpan.FromHours(1));

            return new WavProcessAttr()
            {
                FileLocation = readSas,
                FileNameStem = Path.GetFileNameWithoutExtension(mp3Path),
                Id3Tags = tags
            };
        }

        public static async Task<string> UploadFasterWav(
            WavProcessAttr srcAttr, BlobClient outBlob, ILogger log)
        {
            var wavProcPath = Path.Combine(
                Path.GetDirectoryName(srcAttr.FileLocation), 
                $"{srcAttr.FileNameStem}-{Guid.NewGuid():N}-faster-{srcAttr.Version}.wav");
            try
            {
                await Toolbox.MakeFasterWav(
                    srcAttr.FileLocation, 
                    wavProcPath, 
                    srcAttr.Tempo,
                    log);

                await outBlob.UploadAsync(wavProcPath);
            }
            finally
            {
                DeleteTempFiles(log, wavProcPath);
            }
            // TODO: Replace fixed 1 hour duration?
            return GetReadSAS(outBlob, TimeSpan.FromHours(1));
        }

        public static async Task<string> UploadMp3(
            Mp3ProcessAttr srcAttr, BlobClient outBlob, ILogger log)
        {
            var mp3ProcPath = Path.Combine(
                Path.GetDirectoryName(srcAttr.WavLocation),
                $"{srcAttr.FileNamePrefix}-{Guid.NewGuid():N}{srcAttr.FileNameSuffix}.mp3");
            try
            {
                await Toolbox.EncodeWavToMp3(
                    srcAttr.WavLocation, 
                    mp3ProcPath, 
                    srcAttr.Id3Tags,
                    log);

                await outBlob.UploadAsync(mp3ProcPath);
            }
            finally
            {
                DeleteTempFiles(log, mp3ProcPath);
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


        private static HttpClient httpClient;

        public static async Task<string> DownloadLocalAsync(string uri)
        {
            var ext = Path.GetExtension(new Uri(uri).LocalPath);
            
            var localPath = Path.Combine(
                GetTempWorkFolder(), 
                $"temp-{Guid.NewGuid():N}{ext}");

            httpClient = httpClient ?? new HttpClient();
            
            using (var responseStream = await httpClient.GetStreamAsync(uri))
            using (var localStream = File.OpenWrite(localPath))
            {
                await responseStream.CopyToAsync(localStream);
            }
            return localPath;
        }
    }
}

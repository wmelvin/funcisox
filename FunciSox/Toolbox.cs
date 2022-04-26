using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunciSox
{
    // Class to run the executable audio tools.

    public class Toolbox
    {

        // TODO: Document the parameters being used for each tool below. 

        public static async Task<TagAttr> GetId3Tags(string mp3Path, ILogger log)
        {
            TagAttr tags = new TagAttr();

            string tagQry = "%a|%l|%t|%n|%y|%c";

            //C: \Users\billm\source\repos\FunciSox\FunciSox\Tools\id3.exe - 1 - q "%a|%l|%t|%n|%y|%c" "C:\Temp\LDT43.mp3"

            string tagOut = "";

            // Try version 2 tags.
            var args1 = $"-2 -q \"{tagQry}\" \"{mp3Path}\"";
            tagOut = await RunProcess(GetId3Path(), args1, log);

            if (tagOut.Length == 0 || tagOut.Trim().StartsWith("<empty>"))
            {
                // Try version 1 tags.
                var args2 = $"-1 -q \"{tagQry}\" \"{mp3Path}\"";
                tagOut = await RunProcess(GetId3Path(), args2, log);
            }

            string defaultTag = Path.GetFileNameWithoutExtension(mp3Path);

            if (tagOut.Length == 0)
            {
                tags.Artist = defaultTag;
                tags.Album = defaultTag;
                tags.Title = defaultTag;
            }
            else
            {
                string[] tagItems = tagOut.Split("|");
                // TODO: Check for expected number of elements.
                tags.Artist = tagItems[0].Contains("<empty>") ? defaultTag : tagItems[0];
                tags.Album = tagItems[1].Contains("<empty>") ? defaultTag : tagItems[1];
                tags.Title = tagItems[2].Contains("<empty>") ? defaultTag : tagItems[2];
                tags.TrackNum = tagItems[3].Contains("<empty>") ? "" : tagItems[3];
                tags.Year = tagItems[4].Contains("<empty>") ? "" : tagItems[4];
                tags.Comment = tagItems[5].Contains("<empty>") ? "" : tagItems[5];
            }

            return tags;
        }

        public static async Task ConvertMp3ToWav(string sourceMp3Path, string targetWavPath, ILogger log)
        {
            // Read the source MP3 file and convert it to WAV format.
            // Also apply 'remix -' to convert it to mono.
            // This processing is intended for podcasts, not music.
            //
            var args = $"\"{sourceMp3Path}\" \"{targetWavPath}\" remix -";

            await RunProcess(GetSoxPath(), args, log);
        }

        public static async Task ProcessWav(string sourceWavPath, string targetWavPath, ILogger log)
        {
            
            // Use the 'compand' effect to even out the loudness (maybe?).
            string attack = "0.3";
            string decay = "1";
            string transferFunc = "6:−70,−60,−20";
            string outputGain = "-5";
            string initialVolume = "-90";
            string delay = "0.2";
            string effectArgs = $"{attack},{decay} {transferFunc} {outputGain} {initialVolume} {delay}";

            // TODO: Experiment with the settings, and other effects (compression,
            // contrast, loudness), to get desired results.

            var args = $"\"{sourceWavPath}\" \"{targetWavPath}\" compand {effectArgs}";

            await RunProcess(GetSoxPath(), args, log);
        }

        public static async Task MakeFasterWav(
            string sourceWavPath, 
            string targetWavPath, 
            string new_tempo, 
            ILogger log)
        {
            var args = $"\"{sourceWavPath}\" -b 16 \"{targetWavPath}\" tempo {new_tempo}";
            await RunProcess(GetSoxPath(), args, log);
        }

        public static async Task EncodeWavToMp3(string sourceWavPath, string targetMp3Path, ILogger log)
        {
            var args = $"-V 6 -h \"{sourceWavPath}\" \"{targetMp3Path}\"";
            await RunProcess(GetLamePath(), args, log);
        }

        //public static async Task CopyID3Tags(string sourceMp3Path, string targetMp3Path, ILogger log)
        //{
        //    var args = $"-D \"{sourceMp3Path}\" -1 -2 \"{targetMp3Path}\"";
        //    await RunProcess(GetId3Path(), args, log);
        //}

        private static string GetToolsPath()
        {
            //var loc = typeof(Toolbox).Assembly.Location;  // (1)
            //var uri = new UriBuilder(loc);
            //var path = Uri.UnescapeDataString(uri.Path);
            //var dir = Path.GetDirectoryName(path);
            //return Path.Combine(dir, "..\\Tools");

            // TODO: Get path when deployed.

            var toolsDir = Environment.GetEnvironmentVariable("ToolsDir");
            if (string.IsNullOrEmpty(toolsDir))
            {
                throw new InvalidOperationException($"Missing environment variable 'ToolsDir'.");
            }
            return toolsDir;
        }

        private static string GetSoxPath()
        {
            return Path.Combine(GetToolsPath(), "sox.exe");
        }

        private static string GetLamePath()
        {
            return Path.Combine(GetToolsPath(), "lame.exe");
        }

        private static string GetId3Path()
        {
            return Path.Combine(GetToolsPath(), "id3.exe");
        }

        private static async Task<string> RunProcess(string exe, string args, ILogger log)
        {
            log.LogInformation($"RunProcess: {exe} {args}");

            var psi = new ProcessStartInfo(exe, args);  // (2)
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;

            var sbErr = new StringBuilder();
            var sbOut = new StringBuilder();

            var p = new Process();  // (3)

            p.StartInfo = psi;
            p.ErrorDataReceived += (s, a) => sbErr.AppendLine(a.Data);
            p.OutputDataReceived += (s, a) => sbOut.AppendLine(a.Data);
            p.EnableRaisingEvents = true;
            p.Start();
            p.BeginErrorReadLine();
            p.BeginOutputReadLine();

            await p.WaitForExitAsync();
            if (p.ExitCode != 0)
            {
                log.LogError(sbErr.ToString());
                throw new InvalidOperationException($"Process '{exe}' failed; exit code {p.ExitCode}");
            }
            return sbOut.ToString();
        }
    }
}

/*
Docs:

(1) Assembly.Location: https://docs.microsoft.com/en-us/dotnet/api/system.reflection.assembly.location

(2) ProcessStartInfo: https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo

(3) Process: https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process
*/

using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
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
            string tagQry = "%a|%l|%t|%n|%y|%c";
            string tagOut;

            // Try reading ID3 version 2 tags.
            var args1 = $"-2 -q \"{tagQry}\" \"{mp3Path}\"";
            tagOut = await RunProcess(GetId3Path(), args1, log);

            if (tagOut.Length == 0 || tagOut.Trim().StartsWith("<empty>"))
            {
                // Try reading ID3 version 1 tags.
                var args2 = $"-1 -q \"{tagQry}\" \"{mp3Path}\"";
                tagOut = await RunProcess(GetId3Path(), args2, log);
            }

            // TODO: (1) Handle the empty or "<empty>" tag values correctly.
            // Should split, trim, and check for "<empty>".
            // Add function for that?

            TagAttr tags = new();
            string defaultTag = Path.GetFileNameWithoutExtension(mp3Path);
            string[] tagItems = tagOut.Split("|");
            if (tagItems.Length == 6)
            {
                tags.Artist = tagItems[0].Contains("<empty>") ? defaultTag : tagItems[0];
                tags.Album = tagItems[1].Contains("<empty>") ? defaultTag : tagItems[1];
                tags.Title = tagItems[2].Contains("<empty>") ? defaultTag : tagItems[2];
                tags.TrackNum = tagItems[3].Contains("<empty>") ? "" : tagItems[3];
                tags.Year = tagItems[4].Contains("<empty>") ? "" : tagItems[4];
                tags.Comment = tagItems[5].Contains("<empty>") ? "" : tagItems[5];
            }
            else
            {
                tags.Artist = defaultTag;
                tags.Album = defaultTag;
                tags.Title = defaultTag;
                tags.Comment = "(Could not find ID3 tags in original file.)";
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

        public static async Task EncodeWavToMp3(
            string sourceWavPath, 
            string targetMp3Path, 
            TagAttr id3Tags, 
            ILogger log)
        {
            var id3Args = new StringBuilder();            
            id3Args.Append($" --add-id3v2");
            id3Args.Append($" --ta \"{id3Tags.Artist}\"");
            id3Args.Append($" --tl \"{id3Tags.Album}\"");
            id3Args.Append($" --tt \"{id3Tags.Title}\"");
            id3Args.Append($" --ty \"{id3Tags.Year}\"");
            id3Args.Append($" --tc \"{id3Tags.Comment}\"");
            
            string tagArgs = id3Args.ToString();

            string args = $"-V 6 -h {tagArgs} \"{sourceWavPath}\" \"{targetMp3Path}\"";
            
            await RunProcess(GetLamePath(), args, log);
        }

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

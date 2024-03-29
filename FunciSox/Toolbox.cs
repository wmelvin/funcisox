﻿using Microsoft.Extensions.Logging;
using NAudio.Wave;
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

        private static bool IsEmptyTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return true;
            }
            if (tag.Trim() == "<empty>")
            {
                return true;
            }
            return false;
        }

        private static bool AllEmptyTags(string tags)
        {
            if (string.IsNullOrWhiteSpace(tags))
            {
                return true;
            }
            foreach (string item in tags.Split("|"))
            {
                if (!IsEmptyTag(item))
                {
                    return false;
                }

            }
            return true;
        }

        public static async Task<TagAttr> GetId3Tags(string mp3Path, string tempWorkDir, ILogger log)
        {
            string tagQry = "%a|%l|%t|%n|%y|%c";
            string tagOut;
            string defaultTag = Path.GetFileNameWithoutExtension(mp3Path);

            if (!File.Exists(mp3Path))
            {
                log.LogError("FunciSox/GetId3Tags: Cannot find '{mp3Path}'.", mp3Path);
                return new TagAttr()
                {
                    Artist = defaultTag,
                    Album = defaultTag,
                    Title = defaultTag,
                    Comment = "(Could not find original file.)"
                };
            }

            log.LogInformation("FunciSox/GetId3Tags: Try reading ID3 version 2 tags.");
            var args1 = $"-2 -q \"{tagQry}\" \"{mp3Path}\"";            
            tagOut = await RunProcess(GetId3Path(tempWorkDir), args1, log);

            if (AllEmptyTags(tagOut))
            {
                log.LogInformation("FunciSox/GetId3Tags: Try reading ID3 version 1 tags.");
                var args2 = $"-1 -q \"{tagQry}\" \"{mp3Path}\"";
                tagOut = await RunProcess(GetId3Path(tempWorkDir), args2, log);
            }

            TagAttr tags = new();
            string[] tagItems = tagOut.Split("|");
            if (!AllEmptyTags(tagOut) && (tagItems.Length == 6))
            {
                log.LogInformation("FunciSox/GetId3Tags: Parsing tags.");
                tags.Artist = IsEmptyTag(tagItems[0]) ? defaultTag : tagItems[0];
                tags.Album = IsEmptyTag(tagItems[1]) ? defaultTag : tagItems[1];
                tags.Title = IsEmptyTag(tagItems[2]) ? defaultTag : tagItems[2];
                tags.TrackNum = IsEmptyTag(tagItems[3]) ? "" : tagItems[3];
                tags.Year = IsEmptyTag(tagItems[4]) ? "" : tagItems[4];
                tags.Comment = IsEmptyTag(tagItems[5]) ? "" : tagItems[5];
            }
            else
            {
                log.LogInformation("FunciSox/GetId3Tags: No tags found.");
                tags.Artist = defaultTag;
                tags.Album = defaultTag;
                tags.Title = defaultTag;
                tags.Comment = "(Could not find ID3 tags in original file.)";
            }
            return tags;
        }

        public static void CopyToolsFiles(string tempWorkDir, ILogger log)
        {
            //  Sox.exe requires some DLL files to function.
            //
            //  The publishing process insists on putting DLL files in the bin
            //  directory. It also insists on NOT putting EXE files in the bin
            //  directory. Tried using a Copy task in the csproj file, but 
            //  the files would be automatically moved. Could not find a way to
            //  override this behavior.
            //
            //  These did not work:
            //  * Copying the files as part of the build and publish process.
            //  * Copying the DLL files to the EXE location at the start of
            //    the orchestration.
            //  * Copying the EXE files to the DLL location at the start of
            //    the orchestration.
            //  * Running the sox.exe process with the working directory set to
            //    the location of the DLL files.
            //  * Setting the PATH for the sox.exe process to include the DLL
            //    location.
            //
            //  Copying both the EXE and the DLL files to the temporary work
            //  folder, and running from there, did work.

            string src;
            string dst;

            src = Path.Combine(GetToolsPath(), "sox.exe");
            dst = Path.Combine(tempWorkDir, "sox.exe");
            if (!File.Exists(dst))
            {
                log.LogInformation("COPY '{src}' '{dst}'", src, dst);
                File.Copy(src, dst);
            }

            string dllDir = GetToolsDllPath();

            //  Note: libmad.dll is required when sox.exe is used to convert
            //  MP3 to WAV. It is not required when NAudio is used for that.
            //
            // string[] dlls = new string[] { "cyggomp-1.dll", "cygwin1.dll", "libmad.dll" };

            string[] dlls = new string[] { "cyggomp-1.dll", "cygwin1.dll" };
            
            foreach (string dll in dlls)
            {
                src = Path.Combine(dllDir, dll);
                dst = Path.Combine(tempWorkDir, dll);
                if (!File.Exists(dst))
                {
                    log.LogInformation("COPY '{src}' '{dst}'", src, dst);
                    File.Copy(src, dst);
                }
            }

            // Also copy id3.exe.
            src = Path.Combine(GetToolsPath(), "id3.exe");
            dst = Path.Combine(tempWorkDir, "id3.exe");
            if (!File.Exists(dst))
            {
                log.LogInformation("COPY '{src}' '{dst}'", src, dst);
                File.Copy(src, dst);
            }

        }

        public static void ConvertMp3ToWav(string sourceMp3Path, string targetWavPath, string tempWorkDir, ILogger log)
        {
            // Read the source MP3 file and convert it to WAV format.
            // Also apply 'remix -' to convert it to mono.
            // This processing is intended for podcasts, not music.
            //
            log.LogInformation("Using NAudio for ConvertMp3ToWav.");

            // await RunProcess(GetSoxPath(tempWorkDir), args, log);
            using (var reader = new Mp3FileReader(sourceMp3Path))
            {
                WaveFileWriter.CreateWaveFile(targetWavPath, reader);
            }
        }

        public static async Task ProcessWav(string sourceWavPath, string targetWavPath, string tempWorkDir, ILogger log)
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

            await RunProcess(GetSoxPath(tempWorkDir), args, log);
        }

        public static async Task MakeFasterWav(
            string sourceWavPath, 
            string targetWavPath, 
            string new_tempo, 
            string tempWorkDir, 
            ILogger log)
        {
            var args = $"\"{sourceWavPath}\" -b 16 \"{targetWavPath}\" tempo {new_tempo}";
            //await RunProcess(GetSoxPath(Path.GetDirectoryName(sourceWavPath)), args, log);
            await RunProcess(GetSoxPath(tempWorkDir), args, log);
        }

        public static async Task EncodeWavToMp3(
            string sourceWavPath, 
            string targetMp3Path, 
            TagAttr id3Tags, 
            ILogger log)
        {
            var lameId3Args = new StringBuilder();            
            lameId3Args.Append($" --add-id3v2");
            lameId3Args.Append($" --ta \"{id3Tags.Artist}\"");
            lameId3Args.Append($" --tl \"{id3Tags.Album}\"");
            lameId3Args.Append($" --tt \"{id3Tags.Title}\"");
            lameId3Args.Append($" --tn \"{id3Tags.TrackNum}\"");
            lameId3Args.Append($" --ty \"{id3Tags.Year}\"");
            lameId3Args.Append($" --tc \"{id3Tags.Comment}\"");
            
            string args = $"-V 6 -h {lameId3Args} \"{sourceWavPath}\" \"{targetMp3Path}\"";
            
            await RunProcess(GetLamePath(), args, log);
        }

        private static string GetAssemblyLocation()
        {
            var loc = typeof(Toolbox).Assembly.Location;  // (1)
            var uri = new UriBuilder(loc);
            var path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }

        public static string GetToolsPath()
        {
            var toolsDir = Environment.GetEnvironmentVariable("ToolsDir");
            if (string.IsNullOrEmpty(toolsDir))
            {
                var homeDir = Environment.GetEnvironmentVariable("HOME");
                if (string.IsNullOrEmpty(homeDir))
                {
                    return Path.Combine(GetAssemblyLocation(), "..\\Tools");

                }
                return Path.Combine(homeDir, "site\\wwwroot\\Tools");
            }
            return toolsDir;
        }

        public static string GetToolsDllPath()
        {
            var toolsDir = Environment.GetEnvironmentVariable("ToolsDir");
            if (string.IsNullOrEmpty(toolsDir))
            {
                var homeDir = Environment.GetEnvironmentVariable("HOME");
                if (string.IsNullOrEmpty(homeDir))
                {
                    return Path.Combine(GetAssemblyLocation(), "Tools");

                }
                return Path.Combine(homeDir, "site\\wwwroot\\bin\\Tools");
            }
            return toolsDir;
        }

        private static string GetSoxPath(string tempWorkDir)
        {
            if (string.IsNullOrEmpty(tempWorkDir))
            {
                return Path.Combine(GetToolsPath(), "sox.exe");
            }
            else
            {
                return Path.Combine(tempWorkDir, "sox.exe");
            }
        }

        private static string GetLamePath()
        {
            return Path.Combine(GetToolsPath(), "lame.exe");
        }

        private static string GetId3Path(string tempWorkDir)
        {
            if (string.IsNullOrEmpty(tempWorkDir))
            {
                return Path.Combine(GetToolsPath(), "id3.exe");
            }
            else
            {
                return Path.Combine(tempWorkDir, "id3.exe");
            }
        }

        private static async Task<string> RunProcess(string exe, string args, ILogger log)
        {
            log.LogInformation("FunciSox/RunProcess: {exe} {args}", exe, args);

            if (!File.Exists(exe))
            {
                log.LogError("FunciSox/RunProcess: Cannot find '{exe}'", exe);
                throw new InvalidOperationException($"Cannot find '{exe}'");
            }

            string runDir = Path.GetDirectoryName(exe);

            log.LogInformation("FunciSox/RunProcess: WorkingDirectory = '{runDir}'", runDir);

            var psi = new ProcessStartInfo(exe, args);  // (2)
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            psi.WorkingDirectory = runDir;

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

            string stdout = sbOut.ToString().Trim();
            if (0 < stdout.Length)
            {
                log.LogInformation("FunciSox/RunProcess: Output: {stdout}", stdout);
            }

            if (p.ExitCode != 0)
            {
                log.LogError("FunciSox/RunProcess: ERROR: {sbErr}", sbErr);                
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

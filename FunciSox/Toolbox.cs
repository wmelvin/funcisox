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
        public static async Task ConvertMp3ToWav(string sourceMp3Path, string targetWavPath, ILogger log)
        {
            var args = $"\"{sourceMp3Path}\" \"{targetWavPath}\" -remix";
            await RunSox(args, log);
        }

        private static string GetToolsPath()
        {
            var loc = typeof(Toolbox).Assembly.Location;  // (1)

            var uri = new UriBuilder(loc);
            var path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }

        private static string GetSoxPath()
        {
            return Path.Combine(GetToolsPath(), "sox.exe");
        }

        private static async Task RunSox(string args, ILogger log)
        {
            var soxPath = GetSoxPath();
            var psi = new ProcessStartInfo(soxPath, args);  // (2)

            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardError = true;
            var sb = new StringBuilder();
            var p = new Process();  // (3)

            p.StartInfo = psi;
            p.ErrorDataReceived += (s, a) => sb.AppendLine(a.Data);
            p.EnableRaisingEvents = true;
            p.Start();
            p.BeginErrorReadLine();

            await p.WaitForExitAsync();
            if (p.ExitCode != 0)
            {
                log.LogError(sb.ToString());
                throw new InvalidOperationException($"SoX failed with exit code {p.ExitCode}");
            }
        }

    }
}

/*
Docs:

(1) Assembly.Location: https://docs.microsoft.com/en-us/dotnet/api/system.reflection.assembly.location

(2) ProcessStartInfo: https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo

(3) Process: https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process
*/

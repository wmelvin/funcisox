
using System;

namespace FunciSox
{
    public class SettingsAttr
    {
        public string LocalCopyPath { get; init; }
        public TimeSpan DownloadTimeout { get; init; }
        public bool PreserveTempFiles { get; init; }
    }
}

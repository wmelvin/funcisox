using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunciSox
{
    public class Mp3ProcessAttr
    {
        public string WavLocation { get; set; }
        public string FileNamePrefix { get; set; }
        public string FileNameSuffix { get; set; }
        public TagAttr Id3Tags { get; set; }
        public string LocalCopyPath { get; set; }
    }
}

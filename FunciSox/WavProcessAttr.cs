namespace FunciSox
{
    public class WavProcessAttr
    {
        public string FileLocation { get; set; }
        public string FileNameStem { get; set; }

        // TODO: This is a numeric parameter to SoX, but passing as string for now. Change?
        public string Tempo { get; set; }

        public int Version { get; set; }
        public TagAttr Id3Tags { get; set; }
        public bool PreserveTempFiles { get; set; }
    }
}

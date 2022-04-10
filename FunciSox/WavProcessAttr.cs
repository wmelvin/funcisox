namespace FunciSox
{
    public class WavProcessAttr
    {
        public string FilePath { get; set; }
        public string Tempo { get; set; }
        // TODO: This is a numeric parameter to SoX, but passing as string for now. Change?
        public int Version { get; set; }
    }
}

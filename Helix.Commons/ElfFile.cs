using ELFSharp.ELF;

namespace Helix.Commons
{
    public static class ElfFile
    {
        public static bool IsElfFile(string fileName)
        {
            var fileType = ELFReader.CheckELFType(fileName);
            return fileType != Class.NotELF;
        }
    }
}
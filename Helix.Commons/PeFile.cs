using System;
using System.IO;
using dnlib.DotNet;
using dnlib.IO;
using dnlib.PE;

namespace Helix.Commons
{
    public static class PeFile
    {
        public static bool IsPeFile(string fileName)
        {
            try
            {
                _ = new PEImage(fileName);
                return true;
            }
            catch (BadImageFormatException)
            {
                return false;
            }
            catch (DataReaderException)
            {
                return false;
            }
            catch (IOException)
            {
                //
                // Встречалось исключение dnlib.IO.MemoryMappedDataReaderFactory+MemoryMappedIONotSupportedException
                // (Для файла нулевого размера)
                //
                return false;
            }
        }

        public static bool IsClrFile(string fileName)
        {
            try
            {
                _ = ModuleDefMD.Load(fileName);
                return true;
            }
            catch (BadImageFormatException)
            {
                return false;
            }
        }
    }
}
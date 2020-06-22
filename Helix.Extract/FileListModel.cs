using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Helix.Commons;

namespace Helix.Extract
{
    [SuppressMessage("ReSharper", "NotAccessedField.Global")]
    public class FileListRow
    {
        public ulong Checksum;
        public long Length;
        public string Name;
    }

    public class FileListModel
    {
        private readonly List<FileListRow> _list = new List<FileListRow>();

        public void AddFile(string fileName, string relativeFileName)
        {
            _list.Add(
                new FileListRow
                {
                    Name = relativeFileName,
                    Length = new FileInfo(fileName).Length,
                    Checksum = Magma.CalculateFileChecksum(fileName)
                });
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public IEnumerable<IGrouping<string, FileListRow>> AsTree() =>
            _list
                .OrderBy(fileRow => fileRow.Name)
                .ToLookup(fileRow => Path.GetDirectoryName(fileRow.Name))
                .OrderBy(grouping => grouping.Key);
    }
}
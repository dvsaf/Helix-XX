using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Helix.Compare
{
    public class FileListModel
    {
        public readonly string Title;
        public readonly IEnumerable<IGrouping<string, string>> FileTree;

        public FileListModel(string title, IEnumerable<string> fileTree)
        {
            Title = title;
            FileTree = fileTree
                .OrderBy(fileRow => fileRow)
                .ToLookup(Path.GetDirectoryName)
                .OrderBy(grouping => grouping.Key);
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Helix.Commons;

namespace Helix.Extract
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var (sourceFolder, targetFolder) = ParseCommandLine(args);
            if (string.IsNullOrEmpty(sourceFolder) || string.IsNullOrEmpty(targetFolder))
            {
                Usage();
                return;
            }

            var model = new FileListModel();

            var allFiles = Directory.EnumerateFiles(sourceFolder, "*.*", SearchOption.AllDirectories)
                .ToList();

            Console.WriteLine("Исполнимые файлы:");
            foreach (var sourceFileName in allFiles.Where(fileName => PeFile.IsPeFile(fileName) || ElfFile.IsElfFile(fileName)))
            {
                var relativeFileName = Path.GetRelativePath(sourceFolder, sourceFileName);
                Console.WriteLine("  " + relativeFileName);
                var targetFileName = Path.Combine(targetFolder, relativeFileName);
                Directory.CreateDirectory(Path.GetDirectoryName(targetFileName));
                File.Copy(sourceFileName, targetFileName);

                model.AddFile(targetFileName, relativeFileName);
            }

            Console.WriteLine("Файлы скопированы");

            File.WriteAllText("FileListReport.html", new FileListTemplate(model).TransformText());

            Console.WriteLine("Отчёт создан");
        }

        private static (string sourceFolder, string targetFolder) ParseCommandLine(IEnumerable<string> args)
        {
            string sourceFolder = null, targetFolder = null;

            foreach (var arg in args)
            {
                var parts = arg.Split('=');

                switch (parts[0])
                {
                    case "--source-folder":
                        sourceFolder = parts[1];
                        break;

                    case "--target-folder":
                        targetFolder = parts[1];
                        break;
                }
            }

            return (sourceFolder, targetFolder);
        }

        private static void Usage()
        {
            Console.WriteLine(
                "Программа копирует из заданной папки в целевую папку только исполнимые файлы." + Environment.NewLine +
                "Дополнительно формируется отчёт в файле report.html" + Environment.NewLine +
                "" + Environment.NewLine +
                "Параметры:" + Environment.NewLine +
                "" + Environment.NewLine +
                "  --source-folder=папка  исходная папка" + Environment.NewLine +
                "" + Environment.NewLine +
                "  --target-folder=папка  целевая папка" + Environment.NewLine +
                "");
        }
    }
}
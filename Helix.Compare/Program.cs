using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using Helix.Commons;

namespace Helix.Compare
{
    internal static class Program
    {
        private static string _root462;
        private static string _root48;

        private static void Main(string[] args)
        {
            (_root462, _root48) = ParseCommandLine(args);
            if (string.IsNullOrEmpty(_root462) || string.IsNullOrEmpty(_root48))
            {
                Usage();
                return;
            }

            var clrFilesChanged = CompareFiles();
            clrFilesChanged.Take(1).ToImmutableList().ForEach(CompareTypes);

            Console.WriteLine("Сравнение завершено");
        }

        private static ImmutableList<string> CompareFiles()
        {
            var transform = PageTransform
                .CompileTransform<FileListModel>("FileListTemplate.cshtml");

            var files462 = Directory.EnumerateFiles(_root462, "*.*", SearchOption.AllDirectories)
                .Select(file => Path.GetRelativePath(_root462, file))
                .ToImmutableList();
            var files48 = Directory.EnumerateFiles(_root48, "*.*", SearchOption.AllDirectories)
                .Select(file => Path.GetRelativePath(_root48, file))
                .ToImmutableList();

            var filesAdded = files48.Except(files462).ToImmutableList();
            File.WriteAllText(
                "FilesAdded.html",
                transform(new FileListModel("Добавленные файлы", filesAdded)));
            File.WriteAllLines("NativeFilesAdded.list",
                filesAdded.Where(fileName => !PeFile.IsClrFile(Path.Combine(_root48, fileName))));
            File.WriteAllLines("ClrFilesAdded.list",
                filesAdded.Where(fileName => PeFile.IsClrFile(Path.Combine(_root48, fileName))));

            var filesRemoved = files462.Except(files48).ToImmutableList();
            File.WriteAllText(
                "FilesRemoved.html",
                transform(new FileListModel("Удалённые файлы", filesRemoved)));

            var filesInBoth = files462.Intersect(files48).ToImmutableList();

            var filesEqual = filesInBoth.Where(AreFilesEqual).ToImmutableList();
            File.WriteAllText(
                "FilesEqual.html",
                transform(new FileListModel("Не изменившиеся файлы", filesEqual)));

            var filesChanged = filesInBoth.Except(filesEqual).ToImmutableList();
            File.WriteAllText(
                "FilesChanged.html",
                transform(new FileListModel("Изменившиеся файлы", filesChanged)));

            File.WriteAllLines("NativeFilesChanged.list",
                filesChanged.Where(fileName => !PeFile.IsClrFile(Path.Combine(_root48, fileName))));
            
            var clrFilesChanged =
                filesChanged
                    .Where(fileName => PeFile.IsClrFile(Path.Combine(_root48, fileName)))
                    .ToImmutableList();
            File.WriteAllText(
                "ClrFilesChanged.html",
                transform(new FileListModel("Изменившиеся сборки", clrFilesChanged)));

            Console.WriteLine("Отчёты по файлам сформированы");

            return clrFilesChanged;
        }
        
        private static void PrintList(string title, List<string> list)
        {
            Console.WriteLine("==== " + title);
            list.ForEach(Console.WriteLine);
        }

        private static bool AreFilesEqual(string fileName)
        {
            var fileName462 = Path.Combine(_root462, fileName);
            var fileName48 = Path.Combine(_root48, fileName);
            if (new FileInfo(fileName462).Length != new FileInfo(fileName48).Length)
                return false;

            var fileContent462 = File.ReadAllBytes(fileName462);
            var fileContent48 = File.ReadAllBytes(fileName48);
            return !fileContent462.Where((b, i) => b != fileContent48[i]).Any();
        }

        private static void CompareTypes(string fileName)
        {
            Console.WriteLine("---- " + fileName);

            var file462 = ModuleDefMD.Load(Path.Combine(_root462, fileName));
            var file48 = ModuleDefMD.Load(Path.Combine(_root48, fileName));

            file462.EnableTypeDefFindCache = true;
            file48.EnableTypeDefFindCache = true;

            var types462 = file462.GetTypes().Select(type => type.FullName).ToImmutableList();
            var types48 = file48.GetTypes().Select(type => type.FullName).ToImmutableList();

            var typesAdded = types48.Except(types462).ToImmutableList();
            // PrintList("Добавленные типы", typesAdded);

            var typesRemoved = types462.Except(types48).ToImmutableList();
            // PrintList("Удалённые типы", typesRemoved);

            var typesInBoth = types462.Intersect(types48).ToImmutableList();
            
            var typesCompareModel = new TypesCompareModel(fileName, typesAdded, typesRemoved, typesInBoth,
                new MethodsCompareModel());
            typesInBoth.ForEach(
                typeName => CompareMethods(
                    file462.Find(typeName, false),
                    file48.Find(typeName, false)));
        }

        private static void CompareMethods(TypeDef typeDef462, TypeDef typeDef48)
        {
            Console.WriteLine("~~~~ " + typeDef48.FullName);

            // TODO: constructors?
            var methods462 = typeDef462.Methods.Select(method => method.FullName).ToList();
            var methods48 = typeDef48.Methods.Select(method => method.FullName).ToList();

            var methodsAdded = methods48.Except(methods462).ToList();
            PrintList("Добавленные методы", methodsAdded);
            //methodsAdded.ForEach(methodName => PrintIL(typeDef48.FindMethod(methodName)));

            var methodsRemoved = methods462.Except(methods48).ToList();
            PrintList("Удалённые методы", methodsRemoved);

            // var methodsInBoth = methods462.Intersect(methods48).ToList();
            //methodsInBoth.ForEach(
            //    delegate (string methodName)
            //    {
            //        var methodDef48 = typeDef48.Methods.Single(method => method.FullName == methodName);
            //        if (methodDef48.HasBody)
            //        {
            //            var body48 = methodDef48.MethodBody as CilBody;

            //            var methodDef462 = typeDef462.Methods.Single(method => method.FullName == methodName);
            //            if (!methodDef462.HasBody)
            //                throw new ArgumentException(methodDef462.FullName);
            //            var body462 = methodDef462.MethodBody as CilBody;

            //            //if (!(methodDef.MethodBody is CilBody body) || !body.Instructions.Any())
            //            //    return;
            //            //CompareILs(
            //        }
            //    });
        }


        // private static void PrintIL(MethodDef methodDef)
        // {
        //     Console.WriteLine("++++ " + methodDef.FullName);
        //
        //     if (!(methodDef.MethodBody is CilBody body) || !body.Instructions.Any())
        //         return;
        //
        //     foreach (var instruction in body.Instructions)
        //         Console.WriteLine(instruction.ToString());
        // }
        
        private static (string oldFxFolder, string newFxFolder) ParseCommandLine(IEnumerable<string> args)
        {
            string oldFxFolder = null, newFxFolder = null;

            foreach (var arg in args)
            {
                var parts = arg.Split('=');

                switch (parts[0])
                {
                    case "--old-fx":
                        oldFxFolder = parts[1];
                        break;

                    case "--new-fx":
                        newFxFolder = parts[1];
                        break;
                }
            }

            return (oldFxFolder, newFxFolder);
        }

        private static void Usage()
        {
            Console.WriteLine(
                "Программа сравнивает исполнимые файлы .NET Framework разных версий по составу" + Environment.NewLine +
                "и содержанию. По результатам сравнения формируются отчёты в файлах *.html" + Environment.NewLine +
                "" + Environment.NewLine +
                "Параметры:" + Environment.NewLine +
                "" + Environment.NewLine +
                "  --old-fx=папка  файлы старой версии" + Environment.NewLine +
                "" + Environment.NewLine +
                "  --new-fx=папка  файлы новой версии" + Environment.NewLine +
                "");
        }
    }
}
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Helix.Commons;

namespace Helix.Compare
{
    internal static class Program
    {
        private static string _root462;
        private static string _root48;

        private static readonly Func<FileListModel, string> FileListModelTransform = PageTransform
            .CompileTransform<FileListModel>("FileListTemplate.cshtml");

        private static readonly Func<FilesCompareModel, string> FilesCompareModelTransform = PageTransform
            .CompileTransform<FilesCompareModel>("CompareTemplate.cshtml");

        private static readonly Func<FilesCompareModel, string> FilesCompareModelVerboseTransform = PageTransform
            .CompileTransform<FilesCompareModel>("VerboseCompareTemplate.cshtml");

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
            var files462 = Directory.EnumerateFiles(_root462, "*.*", SearchOption.AllDirectories)
                .Select(file => Path.GetRelativePath(_root462, file))
                .ToImmutableList();
            var files48 = Directory.EnumerateFiles(_root48, "*.*", SearchOption.AllDirectories)
                .Select(file => Path.GetRelativePath(_root48, file))
                .ToImmutableList();

            // var filesAdded = files48.Except(files462).ToImmutableList();
            // File.WriteAllText(
            //     "FilesAdded.html",
            //     FileListModelTransform(new FileListModel("Добавленные файлы", filesAdded)));
            // File.WriteAllLines("NativeFilesAdded.list",
            //     filesAdded.Where(fileName => !PeFile.IsClrFile(Path.Combine(_root48, fileName))));
            // File.WriteAllLines("ClrFilesAdded.list",
            //     filesAdded.Where(fileName => PeFile.IsClrFile(Path.Combine(_root48, fileName))));

            // var filesRemoved = files462.Except(files48).ToImmutableList();
            // File.WriteAllText(
            //     "FilesRemoved.html",
            //     FileListModelTransform(new FileListModel("Удалённые файлы", filesRemoved)));

            var filesInBoth = files462.Intersect(files48).ToImmutableList();

            var filesEqual = filesInBoth.Where(AreFilesEqual).ToImmutableList();
            // File.WriteAllText(
            //     "FilesEqual.html",
            //     FileListModelTransform(new FileListModel("Не изменившиеся файлы", filesEqual)));

            var filesChanged = filesInBoth.Except(filesEqual).ToImmutableList();
            // File.WriteAllText(
            //     "FilesChanged.html",
            //     FileListModelTransform(new FileListModel("Изменившиеся файлы", filesChanged)));

            // File.WriteAllLines("NativeFilesChanged.list",
            //     filesChanged.Where(fileName => !PeFile.IsClrFile(Path.Combine(_root48, fileName))));

            var clrFilesChanged =
                filesChanged
                    .Where(fileName => PeFile.IsClrFile(Path.Combine(_root48, fileName)))
                    .ToImmutableList();
            File.WriteAllText(
                "ClrFilesChanged.html",
                FileListModelTransform(new FileListModel("Изменившиеся сборки", clrFilesChanged)));

            Console.WriteLine("Отчёты по файлам сформированы");

            return clrFilesChanged;
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

            var filesCompareModel = new FilesCompareModel(fileName, typesAdded, typesRemoved,
                typesInBoth.Select(
                    typeName => CompareTypes(
                        file462.Find(typeName, false),
                        file48.Find(typeName, false))));
            File.WriteAllText(
                $"{fileName.Replace(Path.DirectorySeparatorChar, '_')} — Compare.html",
                FilesCompareModelTransform(CleanupFilesCompareModel(filesCompareModel)));
            File.WriteAllText(
                $"{fileName.Replace(Path.DirectorySeparatorChar, '_')} — VerboseCompare.html",
                FilesCompareModelVerboseTransform(filesCompareModel));
        }

        private static FilesCompareModel CleanupFilesCompareModel(FilesCompareModel filesCompareModel) =>
            new FilesCompareModel(
                    filesCompareModel.FileName,
                    filesCompareModel.TypesAdded,
                    filesCompareModel.TypesRemoved,
                    filesCompareModel.TypesCompareResult
                        .Select(CleanupTypesCompareModel)
                        .Where(model =>
                            model.MethodsAdded.Any() 
                            || model.MethodsRemoved.Any()
                            || model.MethodsCompareResult.Any()));

        private static TypesCompareModel CleanupTypesCompareModel(TypesCompareModel typesCompareModel) =>
            new TypesCompareModel(
                    typesCompareModel.TypeName,
                    typesCompareModel.MethodsAdded,
                    typesCompareModel.MethodsRemoved,
                    typesCompareModel.MethodsCompareResult
                        .Where(result => result.MethodBodyStatus != MethodBodyStatus.Same));
        
        private static TypesCompareModel CompareTypes(TypeDef typeDef462, TypeDef typeDef48)
        {
            Console.WriteLine("~~~~ " + typeDef48.FullName);

            // TODO: constructors?
            var methods462 = typeDef462.Methods.Select(method => method.FullName).ToList();
            var methods48 = typeDef48.Methods.Select(method => method.FullName).ToList();

            var methodsAdded = methods48.Except(methods462).ToList();
            // PrintList("Добавленные методы", methodsAdded);
            //methodsAdded.ForEach(methodName => PrintIL(typeDef48.FindMethod(methodName)));

            var methodsRemoved = methods462.Except(methods48).ToList();
            // PrintList("Удалённые методы", methodsRemoved);

            var methodsInBoth = methods462.Intersect(methods48).ToList();

            return new TypesCompareModel(typeDef48.FullName, methodsAdded, methodsRemoved,
                methodsInBoth.Select(
                    methodName => CompareMethods(
                        typeDef462.Methods.Single(method => method.FullName == methodName),
                        typeDef48.Methods.Single(method => method.FullName == methodName))));
        }

        private static MethodsCompareModel CompareMethods(MethodDef methodDef462, MethodDef methodDef48)
        {
            var methodBodyStatus =
                methodDef462.HasBody && !methodDef48.HasBody
                    ? MethodBodyStatus.Removed
                    : !methodDef462.HasBody && methodDef48.HasBody
                        ? MethodBodyStatus.Added
                        : MethodBodyStatus.Same;

            var oldMethodBody = GetMethodBodyText(methodDef462);
            var newMethodBody = GetMethodBodyText(methodDef48);

            if (oldMethodBody.Count != newMethodBody.Count
                || oldMethodBody.Zip(newMethodBody).Any(tuple => tuple.First != tuple.Second))
                methodBodyStatus = MethodBodyStatus.Changed;

            return new MethodsCompareModel(methodDef48.FullName, methodBodyStatus, oldMethodBody, newMethodBody);
        }

        private static ImmutableList<string> GetMethodBodyText(MethodDef methodDef)
        {
            return
                methodDef.MethodBody is CilBody body
                    ? body.Instructions.Select(instruction => instruction.ToString()).ToImmutableList()
                    : ImmutableList<string>.Empty;
        }

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
using System;
using System.Collections.Generic;

namespace Helix.Compare
{
    public class FilesCompareModel
    {
        public readonly string FileName;
        public readonly IEnumerable<string> TypesAdded;
        public readonly IEnumerable<string> TypesRemoved;
        public readonly IEnumerable<TypesCompareModel> TypesCompareResult;

        public FilesCompareModel(string fileName, IEnumerable<string> typesAdded, IEnumerable<string> typesRemoved,
            IEnumerable<TypesCompareModel> typesCompareResult)
        {
            IReadOnlyCollection<string> d;
            
            FileName = fileName;
            TypesAdded = typesAdded;
            TypesRemoved = typesRemoved;
            TypesCompareResult = typesCompareResult;
        }
    }

    public class TypesCompareModel
    {
        public readonly string TypeName;
        public readonly IEnumerable<string> MethodsAdded;
        public readonly IEnumerable<string> MethodsRemoved;
        public readonly IEnumerable<MethodsCompareModel> MethodsCompareResult;

        public TypesCompareModel(string typeName, IEnumerable<string> methodsAdded, IEnumerable<string> methodsRemoved, IEnumerable<MethodsCompareModel> methodsCompareResult)
        {
            TypeName = typeName;
            MethodsAdded = methodsAdded;
            MethodsRemoved = methodsRemoved;
            MethodsCompareResult = methodsCompareResult;
        }
    }

    public enum MethodBodyStatus
    {
        Added,
        Removed,
        Changed,
        Same
    }

    public class MethodsCompareModel
    {
        public readonly string MethodName;
        public readonly MethodBodyStatus MethodBodyStatus;
        public readonly IEnumerable<string> OldMethodBody;
        public readonly IEnumerable<string> NewMethodBody;

        public MethodsCompareModel(string methodName, MethodBodyStatus methodBodyStatus,
            IEnumerable<string> oldMethodBody, IEnumerable<string> newMethodBody)
        {
            MethodName = methodName;
            MethodBodyStatus = methodBodyStatus;
            OldMethodBody = oldMethodBody;
            NewMethodBody = newMethodBody;
        }
    }
}
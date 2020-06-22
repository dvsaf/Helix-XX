using System.Collections.Generic;

namespace Helix.Compare
{
    public class FilesCompareModel
    {
        public readonly string FileName;
        public readonly IEnumerable<string> TypesAdded;
        public readonly IEnumerable<string> TypesRemoved;
        public readonly IEnumerable<string> TypesInBoth;
        public readonly IEnumerable<TypesCompareModel> MethodsCompareModel;

        public FilesCompareModel(string fileName, IEnumerable<string> typesAdded, IEnumerable<string> typesRemoved, IEnumerable<string> typesInBoth, IEnumerable<TypesCompareModel> methodsCompareModel)
        {
            IReadOnlyCollection<string> d;
            
            FileName = fileName;
            TypesAdded = typesAdded;
            TypesRemoved = typesRemoved;
            TypesInBoth = typesInBoth;
            MethodsCompareModel = methodsCompareModel;
        }
    }

    public class TypesCompareModel
    {
        public readonly string MethodName;
        public readonly IEnumerable<string> MethodsAdded;
        public readonly IEnumerable<string> MethodsRemoved;
        public readonly IEnumerable<string> MethodsChanged;

        public TypesCompareModel(string methodName, IEnumerable<string> methodsAdded, IEnumerable<string> methodsRemoved, IEnumerable<string> methodsChanged)
        {
            MethodName = methodName;
            MethodsAdded = methodsAdded;
            MethodsRemoved = methodsRemoved;
            MethodsChanged = methodsChanged;
        }
    }
}
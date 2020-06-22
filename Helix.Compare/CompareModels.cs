using System.Collections.Generic;

namespace Helix.Compare
{
    public class TypesCompareModel
    {
        public readonly string FileName;
        public readonly IEnumerable<string> TypesAdded;
        public readonly IEnumerable<string> TypesRemoved;
        public readonly IEnumerable<string> TypesInBoth;
        public readonly MethodsCompareModel MethodsCompareModel;

        public TypesCompareModel(string fileName, IEnumerable<string> typesAdded, IEnumerable<string> typesRemoved, IEnumerable<string> typesInBoth, MethodsCompareModel methodsCompareModel)
        {
            IReadOnlyCollection<string> d;
            
            FileName = fileName;
            TypesAdded = typesAdded;
            TypesRemoved = typesRemoved;
            TypesInBoth = typesInBoth;
            MethodsCompareModel = methodsCompareModel;
        }
    }

    public class MethodsCompareModel
    {
        
    }
}
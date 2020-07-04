using System.Collections.Generic;

namespace Helix.Compare
{
    public class ChangesToStudyModel
    {
        public readonly string FileName;
        public readonly IEnumerable<string> TypesToStudy;
        public readonly IEnumerable<string> MethodsToStudy;

        public ChangesToStudyModel(string fileName, IEnumerable<string> typesToStudy,
            IEnumerable<string> methodsToStudy)
        {
            FileName = fileName;
            TypesToStudy = typesToStudy;
            MethodsToStudy = methodsToStudy;
        }
    }
}
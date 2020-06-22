using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RazorEngine;
using RazorEngine.Templating;

namespace Helix.Commons
{
    public static class PageTransform
    {
        public static Func<TModel, string> CompileTransform<TModel>(string fileName)
        {
            //
            // Директива @model используется в шаблоне для поддержки IntelliSense.
            // Однако, реализация RazorEngine пытается включить эту директиву в код, что
            // приводит к ошибкам компиляции шаблона.
            // Поэтому перед передачей шаблона на обработку директива удаляется. 
            //
            var template =
                File.ReadAllLines(fileName)
                    .Where(line => !line.StartsWith("@model"))
                    .Aggregate((s1, s2) => s1 + Environment.NewLine + s2);
            var taskAwaiter = Task.Run(() => Engine.Razor.CompileRunner<TModel>(template)).GetAwaiter();

            return model => taskAwaiter.GetResult().Run(model);
        }
    }
}

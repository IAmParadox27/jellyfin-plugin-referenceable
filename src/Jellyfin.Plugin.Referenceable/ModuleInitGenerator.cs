using System.Diagnostics;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Jellyfin.Plugin.Referenceable
{
    [Generator]
    public class ModuleInitGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            Console.WriteLine("Initializing ModuleInitGenerator");
        }

        public void Execute(GeneratorExecutionContext context)
        {
            using Stream? resourceStreamModuleInitializer = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{typeof(ModuleInitGenerator).Namespace}.GeneratorTemplates.ModuleInitializer.cs");
            using Stream? resourceStreamServiceRegistrator = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{typeof(ModuleInitGenerator).Namespace}.GeneratorTemplates.ServiceRegistrator.cs");

            if (resourceStreamModuleInitializer != null)
            {
                using StreamReader reader = new StreamReader(resourceStreamModuleInitializer, Encoding.UTF8);
            
                context.AddSource("ModuleInitializer.g.cs", SourceText.From(reader.ReadToEnd()
                    .Replace("{{namespace}}", context.Compilation.Assembly.Name), Encoding.UTF8));
            }

            if (resourceStreamServiceRegistrator != null)
            {
                using StreamReader reader = new StreamReader(resourceStreamServiceRegistrator, Encoding.UTF8);
            
                context.AddSource("ServiceRegistrator.g.cs", SourceText.From(reader.ReadToEnd()
                    .Replace("{{namespace}}", context.Compilation.Assembly.Name), Encoding.UTF8));
            }
        }
    }
}
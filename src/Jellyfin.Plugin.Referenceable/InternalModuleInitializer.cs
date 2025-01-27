using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Emby.Server.Implementations.Plugins;
using HarmonyLib;

namespace Jellyfin.Plugin.Referenceable
{
    public class InternalModuleInitializer
    {
        private static bool s_initialized = false;
        
        public static void ModuleInitializer()
        {
            if (!s_initialized)
            {
                Harmony harmony = new Harmony("dev.iamparadox.jellyfin.common")!;

                Console.WriteLine("InternalModuleInitializer.Initialize");
                HarmonyMethod createPluginInstanceMethod = new HarmonyMethod(
                    typeof(InternalModuleInitializer).GetMethod(nameof(CreatePluginInstance_PluginManagerPatch)));

                harmony.Patch(typeof(PluginManager).GetMethod("CreatePluginInstance",
                        BindingFlags.NonPublic | BindingFlags.Instance),
                    prefix: createPluginInstanceMethod);

                s_initialized = true;
            }
        }
        
        public static void CreatePluginInstance_PluginManagerPatch(ref Type type)
        {
            string pluginAssemblyFullName = type.Assembly.FullName;
            IEnumerable<Assembly> assembliesContainingType = AssemblyLoadContext.All
                .SelectMany(x => x.Assemblies)
                .Where(x => x.FullName == pluginAssemblyFullName);

            if (assembliesContainingType.Any(x => !x.IsCollectible))
            {
                Assembly assemblyToUse = assembliesContainingType.First(x => !x.IsCollectible);
                
                string typeFullName = type.FullName;
                Type? replacementType = assemblyToUse.GetTypes().FirstOrDefault(x => x.FullName == typeFullName);

                if (replacementType != null)
                {
                    type = replacementType;
                }
            }
        }
    }
}
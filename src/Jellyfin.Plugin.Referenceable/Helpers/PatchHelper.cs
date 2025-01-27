using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using Emby.Server.Implementations.Plugins;
using HarmonyLib;
using Jellyfin.Server;

namespace Jellyfin.Plugin.Referenceable.Helpers
{
    public static class PatchHelper
    {
        private static Harmony s_harmony = new Harmony("dev.iamparadox.jellyfin");

        internal static void SetupPatches()
        {
            HarmonyMethod patchMethod = new HarmonyMethod(typeof(PatchHelper).GetMethod(nameof(Patch_Harmony_Patch), BindingFlags.Static | BindingFlags.NonPublic));
            patchMethod.priority = Priority.First;
            
            HarmonyMethod createPluginInstanceMethod = new HarmonyMethod(typeof(PatchHelper).GetMethod(nameof(Patch_PluginManager_CreatePluginInstance), BindingFlags.Static | BindingFlags.NonPublic));

            HarmonyMethod configureStartupPatchMethod = new HarmonyMethod(typeof(StartupHelper).GetMethod(nameof(StartupHelper.Patch_Startup_Configure), BindingFlags.NonPublic | BindingFlags.Static));
            
            // Setup a patch to stop calls to patch functions when the call has come from another assembly.
            s_harmony.Patch(typeof(Harmony).GetMethod(nameof(Harmony.Patch)), 
                prefix: patchMethod);
            
            // We need to make sure that the plugin instance of a referenceable plugin is created from the non collectible assembly,
            // so we patch this to change the type to the non collectible one where its available.
            s_harmony.Patch(typeof(PluginManager).GetMethod("CreatePluginInstance", BindingFlags.NonPublic | BindingFlags.Instance),
                prefix: createPluginInstanceMethod);
            
            // We patch the Startup.Configure function to allow things to be changed while the app is being setup.
            // Currently the only configurable element is the FileProvider for Default/Static files for /web but 
            // as there are more requirements this will update to include those too.
            s_harmony.Patch(typeof(Startup).GetMethod(nameof(Startup.Configure)),
                prefix: configureStartupPatchMethod);
        }

        private static bool Patch_Harmony_Patch(MethodBase original, HarmonyMethod? prefix = null,
            HarmonyMethod? postfix = null, HarmonyMethod? transpiler = null, HarmonyMethod? finalizer = null)
        {
            // This is probably not perfect and might need changing.
            Assembly? attemptingPatchAssembly = (new StackTrace()).GetFrames().Skip(2).First().GetMethod()?.DeclaringType?.Assembly;
            if (attemptingPatchAssembly != Assembly.GetExecutingAssembly())
            {
                Console.WriteLine($"Patching functions can only be called from assembly '{Assembly.GetExecutingAssembly().FullName}'");
                return false;
            }

            return true;
        }

        private static void Patch_PluginManager_CreatePluginInstance(ref Type type)
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
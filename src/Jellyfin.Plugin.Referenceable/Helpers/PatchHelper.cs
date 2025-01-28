using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using Emby.Server.Implementations;
using Emby.Server.Implementations.Plugins;
using HarmonyLib;
using Jellyfin.Server;
using Microsoft.AspNetCore.Mvc;

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

            HarmonyMethod getApiPluginAssembliesMethod = new HarmonyMethod(typeof(PatchHelper).GetMethod(nameof(Patch_ServerApplicationHost_GetApiPluginAssemblies), BindingFlags.Static | BindingFlags.NonPublic));
            
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
            
            // We patch the ApplicationHost.GetApiPluginAssemblies function to allow us to change the assemblies that are
            // returned for assemblies that have been reloaded into our collectible context.
            s_harmony.Patch(typeof(ApplicationHost).GetMethod(nameof(ApplicationHost.GetApiPluginAssemblies)),
                prefix: getApiPluginAssembliesMethod);
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

        private static bool Patch_ServerApplicationHost_GetApiPluginAssemblies(ref IEnumerable<Assembly> __result, Type[] ____allConcreteTypes)
        {
            // Get the original result back.
            var assemblies = ____allConcreteTypes
                .Where(i => typeof(ControllerBase).IsAssignableFrom(i))
                .Select(i => i.Assembly)
                .Distinct();

            // Group the assemblies by their name.
            var groupedAssemblies = assemblies.GroupBy(x => x.FullName);

            // Do some logic to return a non-collectible assembly if there is one
            // Otherwise return the only one, or first in the case of multiple
            // collectible's.
            var finalAssemblies = groupedAssemblies.Select(x =>
            {
                if (x.Any(y => !y.IsCollectible))
                {
                    return x.First(y => !y.IsCollectible);
                }

                if (x.Count() == 1)
                {
                    return x.Single();
                }

                if (x.Any())
                {
                    return x.First();
                }

                return null;
            }).Where(x => x != null).Select(x => x!);
            
            // Update the return value.
            __result = finalAssemblies;
            
            // Don't execute the original code.
            return false;
        }
    }
}
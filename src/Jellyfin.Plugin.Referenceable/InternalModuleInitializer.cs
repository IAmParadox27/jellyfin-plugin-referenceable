using Jellyfin.Plugin.Referenceable.Helpers;

namespace Jellyfin.Plugin.Referenceable
{
    public class InternalModuleInitializer
    {
        private static bool s_initialized = false;
        
        public static void ModuleInitializer()
        {
            if (!s_initialized)
            {
                PatchHelper.SetupPatches();
                
                s_initialized = true;
            }
        }
    }
}
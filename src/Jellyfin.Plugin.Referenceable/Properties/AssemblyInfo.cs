using System.Diagnostics.CodeAnalysis;
using System.Reflection;

[assembly: AssemblyVersion("1.0.1.0")]
[assembly: AssemblyFileVersion("1.0.1.0")]

[assembly: SuppressMessage("Intentional", "CA2255", Justification = "In order for Jellyfin plugins to be referenceable by other plugins they must be initialized in a non-collectable AssemblyLoadContext. This dll is designed to be a starting point for those types of plugins to avoid individual plugin devs from needing to do this.")]
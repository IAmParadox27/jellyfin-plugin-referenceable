<h1 align="center">Jellyfin Referenceable</h1>
<h2 align="center">A Jellyfin Plugin Library</h2>
<p align="center">
	<img alt="Logo" width="256" height="256" src="https://camo.githubusercontent.com/ab4b1ec289bed0a0ac8dd2828c41b695dbfeaad8c82596339f09ce23b30d3eb3/68747470733a2f2f63646e2e6a7364656c6976722e6e65742f67682f73656c666873742f69636f6e732f776562702f6a656c6c7966696e2e77656270" />
	<br />
	<sub>Custom Logo Coming Soon</sub>
	<br />
	<br />
	<a href="https://github.com/IAmParadox27/jellyfin-plugin-home-sections">
		<img alt="GPL 3.0 License" src="https://img.shields.io/github/license/IAmParadox27/jellyfin-plugin-referenceable.svg" />
	</a>
	<a href="https://github.com/IAmParadox27/jellyfin-plugin-home-sections/releases">
		<img alt="Current Release" src="https://img.shields.io/github/release/IAmParadox27/jellyfin-plugin-referenceable.svg" />
	</a>
	<a href="https://www.nuget.org/packages/Jellyfin.Plugin.Referenceable">
		<img alt="NuGet Release" src="https://img.shields.io/nuget/v/Jellyfin.Plugin.Referenceable" />
	</a>
</p>

## Introduction
Jellyfin Referenceable is a NuGet library that can be included in any Jellyfin plugin to make that plugin referenceable by other plugins. The intent is to allow plugins to be extensible in themselves, like Jellyfin itself is extensible.

The use cases for this can be seen in my other plugins [file-transformation](https://github.com/IAmParadox27/jellyfin-plugin-file-transformation), [plugin-pages](https://github.com/IAmParadox27/jellyfin-plugin-pages) and [home-sections](https://github.com/IAmParadox27/jellyfin-plugin-home-sections).

## Installation

### Which Version?
| Referenceable | Jellyfin |
|---------------|----------|
| 1.1.0         | 10.10.3  |
| 1.2.0         | 10.10.5  |

_There is no version for 10.10.4 as this was only the latest version for 1 day before 10.10.5 was released._

### Prerequisites
- The latest version of this plugin is based on Jellyfin Version `10.10.5`.
- The library **must** be referenced by NuGet not DLL directly. This is due to source generators and targets files being present which only work through NuGet references.

### Referencing this library
Add `Jellyfin.Plugin.Referenceable` from NuGet using the version most applicable from the table above. All versions previous to 1.1.0 do not work correctly and have issues which were only discovered after making the release. 

`OutputItemType` and `GeneratePathProperty` must both be set on the reference. You can use the `<PackageReference>` line below to ensure you are referencing in the correct way.

```xml
<PackageReference Include="Jellyfin.Plugin.Referenceable" Version="1.1.0" OutputItemType="Analyzer" GeneratePathProperty="true" />
```

Since this package generates code that makes use of `Jellyfin.Model` and `Jellyfin.Controller` packages you will also need to ensure that you have the following packages included.

```xml
<PackageReference Include="Jellyfin.Model" Version="10.10.5" />
<PackageReference Include="Jellyfin.Controller" Version="10.10.5" />
```

### Changes from normal plugin development
- Usually when you want to add your own services you would create a class that inherits from `IPluginServiceRegistrator`. With this plugin you should reference `{Your.Plugin.Namespace}.Services.PluginServiceRegistrator`. This class is added as part of a source generator and handles the assembly conversion to ensure you are adding services in a referenceable way.

## Important Notes
- Extra care must be taken when using this library to ensure that all your code is safe. Since it has to reload your plugin's assembly in a way that allows your exposed types to be passed between different assemblies it loses the ability to be unloaded and will crash the server it runs on if an exception is thrown.
- All plugins that reference this library **must** use the same version.
- It is advisable when releasing a plugin that uses this library that you make your users aware of the version of this library it uses so they can make an educated decision about whether any of their other plugins would be incompatible.
- This library brings a library called `0Harmony` into the Jellyfin instance. This is a patching library which allows developers to patch existing functionality using reflection. Care has been taken to ensure that patches can only be performed from this assembly and nothing else.
- This library will be an embedded resource inside all plugins that reference it and will be self injected. Plugin's that use this will see their dll size increase by approximately 2MB.

## Noting the version required

On your GitHub page you can put an image shield like this <img alt="Shield Example" src="https://img.shields.io/badge/JF%20Referenceable-v1.2.0-blue" /> to signify what version of this library you require.

```html
<img alt="Shield Example" src="https://img.shields.io/badge/JF%20Referenceable-v1.2.0-blue" />
```

## Requests
If any functionality is desired to be overridden from Jellyfin's server please open a `feature-request` issue on GitHub.

## FAQ
> Frequent questions will be added here as they are asked.

Ensure that you check the closed issues on GitHub before asking any questions as they may have already been answered.

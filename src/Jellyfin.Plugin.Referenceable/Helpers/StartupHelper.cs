﻿using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using System.Reflection;
using Jellyfin.Plugin.Referenceable.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Prometheus;

namespace Jellyfin.Plugin.Referenceable.Helpers
{
    public delegate IFileProvider FileProviderInstanceDelegate(IServerConfigurationManager serverConfigurationManager, IApplicationBuilder mainApplicationBuilder);
    
    public static class StartupHelper
    {
        private static FileProviderInstanceDelegate? s_webDefaultFilesFileProvider = null;
        private static FileProviderInstanceDelegate? s_webStaticFilesFileProvider = null;

        public static FileProviderInstanceDelegate? WebDefaultFilesFileProvider
        {
            get => s_webDefaultFilesFileProvider;
            set
            {
                // We only allow these to be set once.
                if (s_webDefaultFilesFileProvider == null)
                {
                    s_webDefaultFilesFileProvider = value;
                }
                else
                {
                    throw new AccessViolationException($"Cannot set {nameof(WebDefaultFilesFileProvider)} as it has already been set by assembly '{s_webDefaultFilesFileProvider.Method.DeclaringType?.Assembly.FullName}'.");
                }
            }
        }
        
        public static FileProviderInstanceDelegate? WebStaticFilesFileProvider
        {
            get => s_webStaticFilesFileProvider;
            set
            {
                // We only allow these to be set once.
                if (s_webStaticFilesFileProvider == null)
                {
                    s_webStaticFilesFileProvider = value;
                }
                else
                {
                    throw new AccessViolationException($"Cannot set {nameof(WebStaticFilesFileProvider)} as it has already been set by assembly '{s_webStaticFilesFileProvider.Method.DeclaringType?.Assembly.FullName}'.");
                }
            }
        }

        // When updating Jellyfin version ensure this function is updated to match the targeted version of Jellyfin.
        internal static bool Patch_Startup_Configure(IApplicationBuilder app, IWebHostEnvironment env,
            IConfiguration appConfig, ref object __instance)
        {
            Assembly? jellyfinApiAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.FullName?.Contains("Jellyfin.Api") ?? false);
            
            FieldInfo? serverConfigurationManagerInfo = __instance.GetType().GetField("_serverConfigurationManager", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new ArgumentNullException("__instance.GetType().GetField(\"_serverConfigurationManager\", BindingFlags.Instance | BindingFlags.NonPublic)");
            FieldInfo? serverApplicationHostInfo = __instance.GetType().GetField("_serverApplicationHost", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new ArgumentNullException("__instance.GetType().GetField(\"_serverApplicationHost\", BindingFlags.Instance | BindingFlags.NonPublic)");

            IServerConfigurationManager? serverConfigurationManager = serverConfigurationManagerInfo?.GetValue(__instance) as IServerConfigurationManager;
            IServerApplicationHost? serverApplicationHost = serverApplicationHostInfo?.GetValue(__instance) as IServerApplicationHost;

            app.UseBaseUrlRedirection();

            // Wrap rest of configuration so everything only listens on BaseUrl.
            var config = serverConfigurationManager.GetNetworkConfiguration();
            app.Map(config.BaseUrl, mainApp =>
            {
                if (env.IsDevelopment())
                {
                    mainApp.UseDeveloperExceptionPage();
                }

                mainApp.UseForwardedHeaders();

                // JF Divergence
                if (jellyfinApiAssembly != null)
                {
                    //mainApp.UseMiddleware<ExceptionMiddleware>();
                    Type? exceptionMiddlewareType = jellyfinApiAssembly.GetType("ExceptionMiddleware");
                    if (exceptionMiddlewareType != null)
                    {
                        mainApp.UseMiddleware(exceptionMiddlewareType);
                    }

                    //mainApp.UseMiddleware<ResponseTimeMiddleware>();
                    Type? responseTimeMiddlewareType = jellyfinApiAssembly.GetType("ResponseTimeMiddleware");
                    if (responseTimeMiddlewareType != null)
                    {
                        mainApp.UseMiddleware(responseTimeMiddlewareType);
                    }
                }
                // ~JF Divergence

                mainApp.UseWebSockets();

                mainApp.UseResponseCompression();

                mainApp.UseCors();

                if (config.RequireHttps && serverApplicationHost.ListenWithHttps)
                {
                    mainApp.UseHttpsRedirection();
                }

                // This must be injected before any path related middleware.
                mainApp.UsePathTrim();
                
                if (appConfig.HostWebClient())
                {
                    var extensionProvider = new FileExtensionContentTypeProvider();

                    // subtitles octopus requires .data, .mem files.
                    extensionProvider.Mappings.Add(".data", MediaTypeNames.Application.Octet);
                    extensionProvider.Mappings.Add(".mem", MediaTypeNames.Application.Octet);
                    mainApp.UseDefaultFiles(new DefaultFilesOptions
                    {
                        FileProvider = WebDefaultFilesFileProvider?.Invoke(serverConfigurationManager, mainApp) ?? new PhysicalFileProvider(serverConfigurationManager.ApplicationPaths.WebPath),
                        RequestPath = "/web"
                    });
                    mainApp.UseStaticFiles(new StaticFileOptions
                    {
                        FileProvider = WebStaticFilesFileProvider?.Invoke(serverConfigurationManager, mainApp) ?? new PhysicalFileProvider(serverConfigurationManager.ApplicationPaths.WebPath),
                        RequestPath = "/web",
                        ContentTypeProvider = extensionProvider
                    });

                    mainApp.UseRobotsRedirection();
                }

                mainApp.UseStaticFiles();
                mainApp.UseAuthentication();
                mainApp.UseJellyfinApiSwagger(serverConfigurationManager);
                mainApp.UseQueryStringDecoding();
                mainApp.UseRouting();
                mainApp.UseAuthorization();

                mainApp.UseLanFiltering();
                mainApp.UseIPBasedAccessValidation();
                mainApp.UseWebSocketHandler();
                mainApp.UseServerStartupMessage();

                if (serverConfigurationManager.Configuration.EnableMetrics)
                {
                    // Must be registered after any middleware that could change HTTP response codes or the data will be bad
                    mainApp.UseHttpMetrics();
                }

                mainApp.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                    if (serverConfigurationManager.Configuration.EnableMetrics)
                    {
                        endpoints.MapMetrics();
                    }

                    endpoints.MapHealthChecks("/health");
                });
            });
            
            return false;
        }
    }
}
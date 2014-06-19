namespace NServiceBus.Hosting.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Config;
    using Config.ConfigurationSource;
    using Helpers;
    using Hosting.Profiles;
    using Hosting.Roles;
    using Integration.Azure;
    using Logging;
    using NServiceBus.Azure;

    /// <summary>
    ///     A generic host that can be used to provide hosting services in different environments
    /// </summary>
    public class GenericHost : IHost
    {
        /// <summary>
        ///     Accepts the type which will specify the users custom configuration.
        ///     This type should implement <see cref="IConfigureThisEndpoint" />.
        /// </summary>
        /// <param name="endpointName"></param>
        /// <param name="scannableAssembliesFullName">Assemblies full name that were scanned.</param>
        /// <param name="specifier"></param>
        /// <param name="args"></param>
        /// <param name="defaultProfiles"></param>
        public GenericHost(IConfigureThisEndpoint specifier, string[] args, List<Type> defaultProfiles,
            string endpointName, IEnumerable<string> scannableAssembliesFullName = null)
        {
            this.specifier = specifier;

            if (String.IsNullOrEmpty(endpointName))
            {
                endpointName = specifier.GetType().Namespace ?? specifier.GetType().Assembly.GetName().Name;
            }

            endpointNameToUse = endpointName;
            endpointVersionToUse = FileVersionRetriever.GetFileVersion(specifier.GetType());

            if (scannableAssembliesFullName == null || !scannableAssembliesFullName.Any())
            {
                var assemblyScanner = new AssemblyScanner();
                assemblyScanner.MustReferenceAtLeastOneAssembly.Add(typeof(IHandleMessages<>).Assembly);
                assembliesToScan = assemblyScanner
                    .GetScannableAssemblies()
                    .Assemblies;
            }
            else
            {
                assembliesToScan = scannableAssembliesFullName
                    .Select(Assembly.Load)
                    .ToList();
            }

            args = AddProfilesFromConfiguration(args);

            profileManager = new ProfileManager(assembliesToScan, args, defaultProfiles);
            ProfileActivator.ProfileManager = profileManager;

            roleManager = new RoleManager(assembliesToScan);
        }

        /// <summary>
        ///     Creates and starts the bus as per the configuration
        /// </summary>
        public void Start()
        {
            try
            {
                PerformConfiguration();

                bus = config.CreateBus();
                if (bus != null && !config.Settings.Get<bool>("Endpoint.SendOnly"))
                {
                    bus.Start();
                }

            }
            catch (Exception ex)
            {
                LogManager.GetLogger(typeof(GenericHost)).Fatal("Exception when starting endpoint.", ex);
                throw;
            }
        }

        /// <summary>
        ///     Finalize
        /// </summary>
        public void Stop()
        {
            if (bus != null)
            {
                bus.Shutdown();
                bus.Dispose();

                bus = null;
            }
        }

        /// <summary>
        ///     When installing as windows service (/install), run infrastructure installers
        /// </summary>
        public void Install(string username)
        {
            PerformConfiguration();
            //HACK: to ensure the installer runner performs its installation

            config.EnableInstallers(username);
            config.CreateBus();
        }

        void PerformConfiguration()
        {
            var loggingConfigurers = profileManager.GetLoggingConfigurer();
            foreach (var loggingConfigurer in loggingConfigurers)
            {
                loggingConfigurer.Configure(specifier);
            }

             config = Configure.With(o =>{
                o.EndpointName(endpointNameToUse);
                o.EndpointVersion(() => endpointVersionToUse);
                o.AssembliesToScan(assembliesToScan);

                if (SafeRoleEnvironment.IsAvailable)
                {
                    if (!IsHostedIn.ChildHostProcess())
                    {
                        o.AzureConfigurationSource();
                    }
                }
            });

            roleManager.ConfigureBusForEndpoint(specifier, config);
        }

        private string[] AddProfilesFromConfiguration(IEnumerable<string> args)
        {
            var list = new List<string>(args);

            var configSection = ((IConfigurationSource)new AzureConfigurationSource(new AzureConfigurationSettings())).GetConfiguration<AzureProfileConfig>();

            if (configSection != null)
            {
                var configuredProfiles = configSection.Profiles.Split(',');
                Array.ForEach(configuredProfiles, s => list.Add(s.Trim()));
            }

            return list.ToArray();
        }

        List<Assembly> assembliesToScan;

        ProfileManager profileManager;
        RoleManager roleManager;
        IConfigureThisEndpoint specifier;
        IStartableBus bus;
        Configure config;

        string endpointNameToUse;
        string endpointVersionToUse;
    }
}

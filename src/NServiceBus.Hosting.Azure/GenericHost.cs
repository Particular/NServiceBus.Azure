namespace NServiceBus.Hosting.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Config;
    using Config.ConfigurationSource;
    using Helpers;
    using Profiles;
    using Integration.Azure;
    using Logging;
    using NServiceBus.Azure;
    using Unicast;

    /// <summary>
    ///     A generic host that can be used to provide hosting services in different environments
    /// </summary>
    public class GenericHost : IHost
    {
        /// <summary>
        ///     Accepts the type which will specify the users custom configuration.
        ///     This type should implement <see cref="IConfigureThisEndpoint" />.
        /// </summary>
        /// <param name="scannableAssembliesFullName">Assemblies full name that were scanned.</param>
        /// <param name="specifier"></param>
        /// <param name="args"></param>
        /// <param name="defaultProfiles"></param>
        public GenericHost(IConfigureThisEndpoint specifier, string[] args, List<Type> defaultProfiles,
            IEnumerable<string> scannableAssembliesFullName = null)
        {
            this.specifier = specifier;

            endpointNameToUse = specifier.GetType().Namespace ?? specifier.GetType().Assembly.GetName().Name;
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
        }

        /// <summary>
        ///     Creates and starts the bus as per the configuration
        /// </summary>
        public void Start()
        {
            try
            {
                PerformConfiguration();
                
                if (bus != null && !bus.Settings.Get<bool>("Endpoint.SendOnly"))
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
                bus.Dispose();

                bus = null;
            }
        }

        /// <summary>
        ///     When installing as windows service (/install), run infrastructure installers
        /// </summary>
        public void Install(string username)
        {
            PerformConfiguration(builder => builder.EnableInstallers(username));

            bus.Builder.Dispose();
        }

        void PerformConfiguration(Action<BusConfiguration> moreConfiguration = null)
        {
            var loggingConfigurers = profileManager.GetLoggingConfigurer();
            foreach (var loggingConfigurer in loggingConfigurers)
            {
                loggingConfigurer.Configure(specifier);
            }

            var configuration = new BusConfiguration();

            configuration.EndpointName(endpointNameToUse);
            configuration.EndpointVersion(endpointVersionToUse);
            configuration.AssembliesToScan(assembliesToScan);
           
            if (SafeRoleEnvironment.IsAvailable)
            {
                if (!IsHostedIn.ChildHostProcess())
                {
                    configuration.AzureConfigurationSource();
                }
            }

            if (moreConfiguration != null)
                {
                    moreConfiguration(configuration);
                }

            specifier.Customize(configuration);
            RoleManager.TweakConfigurationBuilder(specifier, configuration);
            bus = (UnicastBus) Bus.Create(configuration);
        }

        string[] AddProfilesFromConfiguration(IEnumerable<string> args)
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
        IConfigureThisEndpoint specifier;
        UnicastBus bus;

        string endpointNameToUse;
        string endpointVersionToUse;
    }
}

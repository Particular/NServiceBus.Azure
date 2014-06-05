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
            Configure.Instance.Initialize();
        }

        void PerformConfiguration()
        {
            var loggingConfigurers = profileManager.GetLoggingConfigurer();
            foreach (var loggingConfigurer in loggingConfigurers)
            {
                loggingConfigurer.Configure(specifier);
            }

            var initialization = specifier as IWantCustomInitialization;
            if (initialization != null)
            {
                try
                {
                    initialization.Init();
                }
                catch (NullReferenceException ex)
                {
                    throw new NullReferenceException(
                        "NServiceBus has detected a null reference in your initialization code." +
                        " This could be due to trying to use NServiceBus.Configure before it was ready." +
                        " One possible solution is to inherit from IWantCustomInitialization in a different class" +
                        " than the one that inherits from IConfigureThisEndpoint, and put your code there.",
                        ex);
                }
            }

            if (config == null)
            {
                config = Configure.With(o =>
                {
                    o.EndpointName(endpointNameToUse);
                    o.AssembliesToScan(assembliesToScan);
                })
                .DefaultBuilder();
            }

            ValidateThatIWantCustomInitIsOnlyUsedOnTheEndpointConfig(config);

            roleManager.ConfigureBusForEndpoint(specifier, config);
        }

        void ValidateThatIWantCustomInitIsOnlyUsedOnTheEndpointConfig(Configure config)
        {
            var problems = config.TypesToScan.Where(t => typeof(IWantCustomInitialization).IsAssignableFrom(t) && !t.IsInterface && !typeof(IConfigureThisEndpoint).IsAssignableFrom(t)).ToList();

            if (!problems.Any())
            {
                return;
            }

            throw new Exception("IWantCustomInitialization is only valid on the same class as ICOnfigureThisEndpoint. Please use INeedInitialization instead. Found types: " + string.Join(",", problems.Select(t => t.FullName)));

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
    }
}

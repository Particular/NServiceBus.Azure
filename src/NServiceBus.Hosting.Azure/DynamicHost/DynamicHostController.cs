using System;
using System.Collections.Generic;
using System.Reflection;
using NServiceBus.Hosting.Profiles;

namespace NServiceBus.Hosting
{
    using Installation;

    public class DynamicHostController : IHost
    {
        private readonly IConfigureThisEndpoint specifier;
        private readonly ProfileManager profileManager;

        private DynamicEndpointLoader loader;
        private DynamicEndpointProvisioner provisioner;
        private DynamicEndpointRunner runner;
        private DynamicHostMonitor monitor;
        private List<EndpointToHost> runningServices;

        public DynamicHostController(IConfigureThisEndpoint specifier, string[] requestedProfiles, List<Type> defaultProfiles, string endpointName)
        {
            this.specifier = specifier;
            Configure.Instance.Settings.Set("EndpointName", endpointName);

            var assembliesToScan = new List<Assembly> {GetType().Assembly};

            profileManager = new ProfileManager(assembliesToScan, requestedProfiles, defaultProfiles);
        }

        public void Start()
        {
            Configure config = null;

            if (specifier is IWantCustomInitialization)
            {
                try
                {
                   config = (specifier as IWantCustomInitialization).Init();
                }
                catch (NullReferenceException ex)
                {
                    throw new NullReferenceException("NServiceBus has detected a null reference in your initalization code." +
                        " This could be due to trying to use NServiceBus.Configure before it was ready." +
                        " One possible solution is to inherit from IWantCustomInitialization in a different class" +
                        " than the one that inherits from IConfigureThisEndpoint, and put your code there.", ex);
                }
            }

            if (config == null)
            {
                config = Configure.With(o => o.AssembliesToScan(GetType().Assembly))
                   .DefaultBuilder();
            }

            config.AzureConfigurationSource();
            config.Configurer.ConfigureComponent<DynamicEndpointLoader>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<DynamicEndpointProvisioner>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<DynamicEndpointRunner>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<DynamicHostMonitor>(DependencyLifecycle.SingleInstance);

            var configSection = config.Settings.GetConfigSection<DynamicHostControllerConfig>() ?? new DynamicHostControllerConfig();

            config.Configurer.ConfigureProperty<DynamicEndpointLoader>(t => t.ConnectionString, configSection.ConnectionString);
            config.Configurer.ConfigureProperty<DynamicEndpointLoader>(t => t.Container, configSection.Container);
            config.Configurer.ConfigureProperty<DynamicEndpointProvisioner>(t => t.LocalResource, configSection.LocalResource);
            config.Configurer.ConfigureProperty<DynamicEndpointProvisioner>(t => t.RecycleRoleOnError, configSection.RecycleRoleOnError);
            config.Configurer.ConfigureProperty<DynamicEndpointRunner>(t => t.RecycleRoleOnError, configSection.RecycleRoleOnError);
            config.Configurer.ConfigureProperty<DynamicEndpointRunner>(t => t.TimeToWaitUntilProcessIsKilled, configSection.TimeToWaitUntilProcessIsKilled);
            config.Configurer.ConfigureProperty<DynamicHostMonitor>(t => t.Interval, configSection.UpdateInterval);

            profileManager.ActivateProfileHandlers(config);

            loader = config.Builder.Build<DynamicEndpointLoader>();
            provisioner = config.Builder.Build<DynamicEndpointProvisioner>();
            runner = config.Builder.Build<DynamicEndpointRunner>();

            var endpointsToHost = loader.LoadEndpoints();
            if (endpointsToHost == null) return;

            runningServices = new List<EndpointToHost>(endpointsToHost);

            provisioner.Provision(runningServices);

            runner.Start(runningServices);
            

            if (!configSection.AutoUpdate) return;

            monitor = config.Builder.Build<DynamicHostMonitor>();
            monitor.UpdatedEndpoints += UpdatedEndpoints;
            monitor.NewEndpoints += NewEndpoints;
            monitor.RemovedEndpoints += RemovedEndpoints;
            monitor.Monitor(runningServices);
            monitor.Start();
        }

        public void Stop()
        {
            if (monitor != null)
                monitor.Stop();

            if (runner != null)
                runner.Stop(runningServices);
        }

        public void Install(string username)
        {
            //todo -yves
        }

        public void UpdatedEndpoints(object sender, EndpointsEventArgs e)
        {
            runner.Stop(e.Endpoints);
            provisioner.Remove(e.Endpoints);
            provisioner.Provision(e.Endpoints);
            runner.Start(e.Endpoints);
        }

        public void NewEndpoints(object sender, EndpointsEventArgs e)
        {
            provisioner.Provision(e.Endpoints);
            runner.Start(e.Endpoints);
            monitor.Monitor(e.Endpoints);
            runningServices.AddRange(e.Endpoints);
        }

        public void RemovedEndpoints(object sender, EndpointsEventArgs e)
        {
            monitor.StopMonitoring(e.Endpoints);
            runner.Stop(e.Endpoints);
            provisioner.Remove(e.Endpoints);
            foreach (var endpoint in e.Endpoints)
                runningServices.Remove(endpoint);
        }
    }
}

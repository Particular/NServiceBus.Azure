using System;
using System.Collections.Generic;
using System.Reflection;
using NServiceBus.Hosting.Profiles;

namespace NServiceBus.Hosting.Azure
{
    internal class DynamicHostController : IHost
    {
        private readonly IConfigureThisEndpoint specifier;
        private readonly ProfileManager profileManager;

        private DynamicEndpointLoader loader;
        private DynamicEndpointProvisioner provisioner;
        private DynamicEndpointRunner runner;
        private DynamicHostMonitor monitor;
        private List<EndpointToHost> runningServices;

        public DynamicHostController(IConfigureThisEndpoint specifier, string[] requestedProfiles, List<Type> defaultProfiles)
        {
            this.specifier = specifier;
            
            var assembliesToScan = new List<Assembly> {GetType().Assembly};

            profileManager = new ProfileManager(assembliesToScan, requestedProfiles, defaultProfiles);
        }

        public void Start()
        {
            var config = Configure.With(o =>
                {
                    o.AssembliesToScan(GetType().Assembly);
                    o.AzureConfigurationSource();

                    specifier.Customize(o);
                });
            

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

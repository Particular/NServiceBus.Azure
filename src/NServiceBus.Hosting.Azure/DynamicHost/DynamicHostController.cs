using NServiceBus.Transports;

namespace NServiceBus.Hosting.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Configuration.AdvanceExtensibility;
    using Profiles;
    using Unicast;

    class DynamicHostController : IHost
    {
        IConfigureThisEndpoint specifier;
        ProfileManager profileManager;

        DynamicEndpointLoader loader;
        DynamicEndpointProvisioner provisioner;
        DynamicEndpointRunner runner;
        DynamicHostMonitor monitor;
        List<EndpointToHost> runningServices;

        public DynamicHostController(IConfigureThisEndpoint specifier, string[] requestedProfiles, List<Type> defaultProfiles)
        {
            this.specifier = specifier;
            
            var assembliesToScan = new List<Assembly> {GetType().Assembly};

            profileManager = new ProfileManager(assembliesToScan, requestedProfiles, defaultProfiles);
        }

        public void Start()
        {
            DynamicHostControllerConfig configSection = null;

            var o = new BusConfiguration();
            o.AssembliesToScan(GetType().Assembly);
            o.AzureConfigurationSource();
            o.RegisterComponents(Configurer =>
            {
                Configurer.ConfigureComponent<DynamicEndpointLoader>(DependencyLifecycle.SingleInstance);
                Configurer.ConfigureComponent<DynamicEndpointProvisioner>(DependencyLifecycle.SingleInstance);
                Configurer.ConfigureComponent<DynamicEndpointRunner>(DependencyLifecycle.SingleInstance);
                Configurer.ConfigureComponent<DynamicHostMonitor>(DependencyLifecycle.SingleInstance);

                configSection = o.GetSettings().GetConfigSection<DynamicHostControllerConfig>() ?? new DynamicHostControllerConfig();

                Configurer.ConfigureProperty<DynamicEndpointLoader>(t => t.ConnectionString, configSection.ConnectionString);
                Configurer.ConfigureProperty<DynamicEndpointLoader>(t => t.Container, configSection.Container);
                Configurer.ConfigureProperty<DynamicEndpointProvisioner>(t => t.LocalResource, configSection.LocalResource);
                Configurer.ConfigureProperty<DynamicEndpointProvisioner>(t => t.RecycleRoleOnError, configSection.RecycleRoleOnError);
                Configurer.ConfigureProperty<DynamicEndpointRunner>(t => t.RecycleRoleOnError, configSection.RecycleRoleOnError);
                Configurer.ConfigureProperty<DynamicEndpointRunner>(t => t.TimeToWaitUntilProcessIsKilled, configSection.TimeToWaitUntilProcessIsKilled);
                Configurer.ConfigureProperty<DynamicHostMonitor>(t => t.Interval, configSection.UpdateInterval);
            });

            o.UsePersistence<AzureStoragePersistence>();
            o.DiscardFailedMessagesInsteadOfSendingToErrorQueue();

            profileManager.ActivateProfileHandlers(o);
            specifier.Customize(o);

            var bus = (UnicastBus)Bus.CreateSendOnly(o);

            loader = bus.Builder.Build<DynamicEndpointLoader>();
            provisioner = bus.Builder.Build<DynamicEndpointProvisioner>();
            runner = bus.Builder.Build<DynamicEndpointRunner>();

            var endpointsToHost = loader.LoadEndpoints();
            if (endpointsToHost == null) return;

            runningServices = new List<EndpointToHost>(endpointsToHost);

            provisioner.Provision(runningServices);

            runner.Start(runningServices);

            if (!configSection.AutoUpdate) return;

            monitor = bus.Builder.Build<DynamicHostMonitor>();
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

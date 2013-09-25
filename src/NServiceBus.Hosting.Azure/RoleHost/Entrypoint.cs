using Microsoft.WindowsAzure.ServiceRuntime;
using NServiceBus.Config;
using NServiceBus.Hosting.Helpers;
using NServiceBus.Integration.Azure;
using System.Threading;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;

namespace NServiceBus.Hosting.Azure
{
    /// <summary>
    /// A host implementation for the Azure cloud platform
    /// </summary>
    public class RoleEntryPoint : Microsoft.WindowsAzure.ServiceRuntime.RoleEntryPoint
    {
        const string ProfileSetting = "AzureProfileConfig.Profiles";
        const string EndpointConfigurationType = "EndpointConfigurationType";
        IHost host;
        readonly ManualResetEvent waitForStop = new ManualResetEvent(false);
        bool doNotReturnFromRun = true;

        public RoleEntryPoint() : this(true)
        {
        }

        public RoleEntryPoint(bool doNotReturnFromRun)
        {
            this.doNotReturnFromRun = doNotReturnFromRun;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;
        }

        public override bool OnStart()
        {
            var azureSettings = new AzureConfigurationSettings();

            var requestedProfiles = GetRequestedProfiles(azureSettings);
            var endpointConfigurationType = GetEndpointConfigurationType(azureSettings);

            AssertThatEndpointConfigurationTypeHasDefaultConstructor(endpointConfigurationType);

            var specifier = (IConfigureThisEndpoint) Activator.CreateInstance(endpointConfigurationType);


            var endpointName = SafeRoleEnvironment.IsAvailable
                ? RoleEnvironment.CurrentRoleInstance.Role.Name
                : GetType().Name;

            if (specifier is AsA_Host)
            {
                host = new DynamicHostController(specifier, requestedProfiles, new List<Type> {typeof(Development)},
                    endpointName);
            }
            else
            {
                host = new GenericHost(specifier, requestedProfiles,
                    new List<Type> {typeof(Development), typeof(OnAzureTableStorage)}, endpointName);
            }

            return true;
        }

        static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Trace.WriteLine("Unhandled exception occured: " + e.ExceptionObject.ToString());
        }

        public override void Run()
        {
            host.Start();
            if (doNotReturnFromRun) waitForStop.WaitOne();
        }

        public override void OnStop()
        {
            host.Stop();
            waitForStop.Set();
        }

        static void AssertThatEndpointConfigurationTypeHasDefaultConstructor(Type type)
        {
            var constructor = type.GetConstructor(Type.EmptyTypes);

            if (constructor == null)
                throw new InvalidOperationException(
                    "Endpoint configuration type needs to have a default constructor: " + type.FullName);
        }

        static string[] GetRequestedProfiles(IAzureConfigurationSettings azureSettings)
        {
            string requestedProfileSetting;
            if (azureSettings.TryGetSetting(ProfileSetting, out requestedProfileSetting))
            {
                var requestedProfiles = requestedProfileSetting.Split(' ');
                requestedProfiles = AddProfilesFromConfiguration(requestedProfiles);
                return requestedProfiles;
            }
            return new string[0];
        }

        static Type GetEndpointConfigurationType(AzureConfigurationSettings settings)
        {
            string endpoint;
            if (settings.TryGetSetting(EndpointConfigurationType, out endpoint))
            {
                var endpointType = Type.GetType(endpoint, false);
                if (endpointType == null)
                    throw new ConfigurationErrorsException(
                        string.Format(
                            "The 'EndpointConfigurationType' entry in the role config has specified to use the type '{0}' but that type could not be loaded.",
                            endpoint));

                return endpointType;
            }

            var endpoints = ScanAssembliesForEndpoints().ToList();

            ValidateEndpoints(endpoints);

            return endpoints.First();
        }

        static IEnumerable<Type> ScanAssembliesForEndpoints()
        {
            return AssemblyScanner.GetScannableAssemblies().Assemblies.SelectMany(
                assembly => assembly.GetTypes().Where(
                    t => typeof(IConfigureThisEndpoint).IsAssignableFrom(t)
                         && t != typeof(IConfigureThisEndpoint)
                         && !t.IsAbstract));
        }

        static void ValidateEndpoints(IList<Type> endpointConfigurationTypes)
        {
            var count = endpointConfigurationTypes.Count();
            if (count == 0)
            {
                throw new InvalidOperationException("No endpoint configuration found in scanned assemlies. " +
                                                    "This usually happens when NServiceBus fails to load your assembly containing IConfigureThisEndpoint." +
                                                    " Try specifying the type explicitly in the roles config using the appsetting key: EndpointConfigurationType, " +
                                                    "Scanned path: " + AppDomain.CurrentDomain.BaseDirectory);
            }

            if (count > 1)
            {
                throw new InvalidOperationException("Host doesn't support hosting of multiple endpoints. " +
                                                    "Endpoint classes found: " +
                                                    string.Join(", ",
                                                        endpointConfigurationTypes.Select(
                                                            e => e.AssemblyQualifiedName).ToArray()) +
                                                    " You may have some old assemblies in your runtime directory." +
                                                    " Try right-clicking your VS project, and selecting 'Clean'."
                    );

            }
        }

        static string[] AddProfilesFromConfiguration(IEnumerable<string> args)
        {
            var list = new List<string>(args);

            var configSection = Configure.GetConfigSection<AzureProfileConfig>();

            if (configSection != null)
            {
                var configuredProfiles = configSection.Profiles.Split(',');
                Array.ForEach(configuredProfiles, s => list.Add(s.Trim()));
            }

            return list.ToArray();
        }
    }
}
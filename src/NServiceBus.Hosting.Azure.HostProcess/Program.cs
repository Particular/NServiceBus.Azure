namespace NServiceBus.Hosting.Azure.HostProcess
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using NServiceBus.Hosting.Helpers;
    using Topshelf;
    using Topshelf.Configuration;
    using Topshelf.Internal;

    class Program
    {
        static List<Assembly> scannedAssemblies;

        static void Main(string[] args)
        {
            var commandLineArguments = Parser.ParseArgs(args);
            var arguments = new HostArguments(commandLineArguments);

            if (arguments.Help != null)
            {
                DisplayHelpContent();

                return;
            }

            var endpointConfigurationType = GetEndpointConfigurationType(arguments);

            AssertThatEndpointConfigurationTypeHasDefaultConstructor(endpointConfigurationType);

            var endpointConfigurationFile = GetEndpointConfigurationFile(endpointConfigurationType);

            var endpointConfiguration = Activator.CreateInstance(endpointConfigurationType);

            EndpointId = GetEndpointId(endpointConfiguration);

            scannedAssemblies = scannedAssemblies ?? new List<Assembly>();
            var assemblylist = string.Join(";", scannedAssemblies.Select((s => s.ToString())));
            args = args.Concat(new[]{String.Format(@"/scannedAssemblies={0}", assemblylist)}).ToArray();

            AppDomain.CurrentDomain.SetupInformation.AppDomainInitializerArguments = args;

            var cfg = RunnerConfigurator.New(x =>
            {
                x.ConfigureServiceInIsolation<WindowsHost>(endpointConfigurationType.AssemblyQualifiedName, c =>
                {
                    c.ConfigurationFile(endpointConfigurationFile);
                    c.CommandLineArguments(args, () => SetHostServiceLocatorArgs);
                    c.WhenStarted(service => service.Start());
                    c.WhenStopped(service => service.Stop());
                    c.CreateServiceLocator(() => new HostServiceLocator());
                });

                if (arguments.Username != null && arguments.Password != null)
                {
                    x.RunAs(arguments.Username.Value, arguments.Password.Value);
                }
                else
                {
                    x.RunAsLocalSystem();
                }

                if (arguments.StartManually != null)
                {
                    x.DoNotStartAutomatically();
                }

                x.SetDisplayName(arguments.DisplayName != null ? arguments.DisplayName.Value : EndpointId);
                x.SetServiceName(arguments.ServiceName != null ? arguments.ServiceName.Value : EndpointId);
                x.SetDescription(arguments.Description != null ? arguments.Description.Value : "NServiceBus Message Endpoint Host Service");
                x.DependencyOnMsmq();

                var serviceCommandLine = commandLineArguments.CustomArguments.AsCommandLine();

                if (arguments.ServiceName != null)
                {
                    serviceCommandLine += " /serviceName:\"" + arguments.ServiceName.Value + "\"";
                }

                x.SetServiceCommandLine(serviceCommandLine);

                if (arguments.DependsOn != null)
                {
                    var dependencies = arguments.DependsOn.Value.Split(',');

                    foreach (var dependency in dependencies)
                    {
                        if (dependency.ToUpper() == KnownServiceNames.Msmq)
                        {
                            continue;
                        }

                        x.DependsOn(dependency);
                    }
                }
            });

            Runner.Host(cfg, args);
        }

        static void DisplayHelpContent()
        {
            try
            {
                var stream = Assembly.GetCallingAssembly().GetManifestResourceStream("NServiceBus.Hosting.Azure.HostProcess.Content.Help.txt");

                if (stream != null)
                {
                    var helpText = new StreamReader(stream).ReadToEnd();

                    Console.WriteLine(helpText);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Gives an identifier for this endpoint
        /// </summary>
        public static string EndpointId { get; set; }

        static void SetHostServiceLocatorArgs(string[] args)
        {
            HostServiceLocator.Args = args;
        }

        static void AssertThatEndpointConfigurationTypeHasDefaultConstructor(Type type)
        {
            var constructor = type.GetConstructor(Type.EmptyTypes);

            if (constructor == null)
                throw new InvalidOperationException("Endpoint configuration type needs to have a default constructor: " + type.FullName);
        }

        static string GetEndpointConfigurationFile(Type endpointConfigurationType)
        {
            return Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                endpointConfigurationType.Assembly.ManifestModule.Name + ".config");
        }

        /// <summary>
        /// Gives a string which serves to identify the endpoint.
        /// </summary>
        /// <param name="endpointConfiguration"></param>
        /// <returns></returns>
        public static string GetEndpointId(object endpointConfiguration)
        {
            var endpointName = endpointConfiguration.GetType().FullName;
            return string.Format("{0}_v{1}", endpointName, endpointConfiguration.GetType().Assembly.GetName().Version);
        }

        static Type GetEndpointConfigurationType(HostArguments arguments)
        {
            if (arguments.EndpointConfigurationType != null)
            {
                var t = arguments.EndpointConfigurationType.Value;
                if (t != null)
                {
                    var endpointType = Type.GetType(t, false);
                    if (endpointType == null)
                    {
                        var message = string.Format("Command line argument 'endpointConfigurationType' has specified to use the type '{0}' but that type could not be loaded.", t);
                        throw new ConfigurationErrorsException(message);
                    }

                    return endpointType;
                }
            }

            var endpoint = ConfigurationManager.AppSettings["EndpointConfigurationType"];
            if (endpoint != null)
            {
                var endpointType = Type.GetType(endpoint, false);
                if (endpointType == null)
                {
                    var message = string.Format("The 'EndpointConfigurationType' entry in the NServiceBus.Host.exe.config has specified to use the type '{0}' but that type could not be loaded.", endpoint);
                    throw new ConfigurationErrorsException(message);
                }

                return endpointType;
            }

            var endpoints = ScanAssembliesForEndpoints();

            ValidateEndpoints(endpoints);

            return endpoints.First();
        }

        static IEnumerable<Type> ScanAssembliesForEndpoints()
        {
            if (scannedAssemblies == null)
            {
                var assemblyScanner = new AssemblyScanner
                {
                    ThrowExceptions = false
                };
                assemblyScanner.MustReferenceAtLeastOneAssembly.Add(typeof(IHandleMessages<>).Assembly);
                assemblyScanner.MustReferenceAtLeastOneAssembly.Add(typeof(IConfigureThisEndpoint).Assembly);
                assemblyScanner.MustReferenceAtLeastOneAssembly.Add(typeof(Program).Assembly);

                scannedAssemblies = assemblyScanner.GetScannableAssemblies().Assemblies;
            }

            return scannedAssemblies
                .SelectMany(assembly => assembly.GetTypes().Where(
                t => typeof(IConfigureThisEndpoint).IsAssignableFrom(t)
                     && t != typeof(IConfigureThisEndpoint)
                     && !t.IsAbstract));
          
        }

        static void ValidateEndpoints(IEnumerable<Type> endpointConfigurationTypes)
        {
            if (endpointConfigurationTypes.Count() == 0)
            {
                throw new InvalidOperationException("No endpoint configuration found in scanned assemblies. " +
                                                    "This usually happens when NServiceBus fails to load your assembly containing IConfigureThisEndpoint." +
                                                    " Try specifying the type explicitly in the NServiceBus.Host.exe.config using the appsetting key: EndpointConfigurationType, " +
                                                    "Scanned path: " + AppDomain.CurrentDomain.BaseDirectory);
            }

            if (endpointConfigurationTypes.Count() > 1)
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

    }
}

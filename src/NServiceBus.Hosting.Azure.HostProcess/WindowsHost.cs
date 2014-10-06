namespace NServiceBus.Hosting.Azure.HostProcess
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A windows implementation of the NServiceBus hosting solution
    /// </summary>
    public class WindowsHost : MarshalByRefObject
    {
        GenericHost genericHost;

        /// <summary>
        /// Accepts the type which will specify the users custom configuration.
        /// This type should implement <see cref="IConfigureThisEndpoint"/>.
        /// </summary>
        /// <param name="endpointType"></param>
        /// <param name="args"></param>
        /// <param name="scannableAssembliesFullName"></param>
        public WindowsHost(Type endpointType, string[] args, IEnumerable<string> scannableAssembliesFullName)
        {
            var specifier = (IConfigureThisEndpoint)Activator.CreateInstance(endpointType);

            Program.EndpointId = Program.GetEndpointId(specifier);

            genericHost = new GenericHost(specifier, args, new List<Type> { typeof(Development) }, scannableAssembliesFullName);
        }

        /// <summary>
        /// Does startup work.
        /// </summary>
        public void Start()
        {
            genericHost.Start();
        }

        /// <summary>
        /// Does shutdown work.
        /// </summary>
        public void Stop()
        {
            genericHost.Stop();
        }


        
    }
}
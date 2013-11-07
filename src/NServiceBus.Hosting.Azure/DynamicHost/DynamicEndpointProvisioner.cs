using System;
using System.Collections.Generic;
using System.IO;
using NServiceBus.Logging;

namespace NServiceBus.Hosting
{
    using Config;

    internal class DynamicEndpointProvisioner
    {
        public string LocalResource { get; set; }

        private readonly ILog logger = LogManager.GetLogger(typeof(DynamicEndpointRunner));

        public bool RecycleRoleOnError { get; set; }

        public void Provision(IEnumerable<EndpointToHost> endpoints)
        {
            try
            {
                string localResource = SafeRoleEnvironment.GetRootPath(LocalResource);

                foreach (var endpoint in endpoints)
                {
                    endpoint.ExtractTo(localResource);

                    endpoint.EntryPoint = Path.Combine(localResource, endpoint.EndpointName, "NServiceBus.Hosting.Azure.HostProcess.exe");
                }
            }
            catch (Exception e)
            {
                logger.Error(e.Message);

                if (RecycleRoleOnError) SafeRoleEnvironment.RequestRecycle();
            }
            
        }

        public void Remove(IEnumerable<EndpointToHost> endpoints)
        {
            string localResource;
            if (!SafeRoleEnvironment.TryGetRootPath(LocalResource, out localResource)) return;

            foreach (var endpoint in endpoints)
            {
                var path = Path.Combine(localResource, endpoint.EndpointName);
                Directory.Delete(path, true);
            }
        }
    }
}
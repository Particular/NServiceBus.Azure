namespace NServiceBus.Hosting.Azure
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Logging;
    using Config;

    class DynamicEndpointProvisioner
    {
        public string LocalResource { get; set; }

        ILog logger = LogManager.GetLogger(typeof(DynamicEndpointRunner));

        public bool RecycleRoleOnError { get; set; }

        public void Provision(IEnumerable<EndpointToHost> endpoints)
        {
            try
            {
                var localResource = SafeRoleEnvironment.GetRootPath(LocalResource);

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
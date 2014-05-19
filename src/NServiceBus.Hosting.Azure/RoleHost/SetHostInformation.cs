namespace NServiceBus.Hosting.Azure
{
    using System;
    using System.Security.Cryptography;
    using System.Text;
    using Config;
    using Unicast;
    
    public class SetHostInformation : IWantToRunWhenConfigurationIsComplete
    {
        readonly UnicastBus unicastBus;

        public SetHostInformation(UnicastBus unicastBus)
        {
            this.unicastBus = unicastBus;
        }

        public void Run(Configure config)
        {
            if (SafeRoleEnvironment.IsAvailable)
            {
                var host = SafeRoleEnvironment.CurrentRoleName;
                var instance = SafeRoleEnvironment.CurrentRoleInstanceId;
                var hostId = DeterministicGuid(instance, host);

#pragma warning disable 618
                var hostInfo = new HostInformation(hostId, host, instance);
#pragma warning restore 618

                unicastBus.HostInformation = hostInfo;
            }
        }

        static Guid DeterministicGuid(params object[] data)
        {
            // use MD5 hash to get a 16-byte hash of the string
            using (var provider = new MD5CryptoServiceProvider())
            {
                var inputBytes = Encoding.Default.GetBytes(String.Concat(data));
                var hashBytes = provider.ComputeHash(inputBytes);
                // generate a guid from the hash:
                return new Guid(hashBytes);
            }
        }

    }
}
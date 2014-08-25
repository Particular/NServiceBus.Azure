namespace NServiceBus
{
    using System;
    using System.Linq;

    static class RoleManager
    {
        public static void TweakConfigurationBuilder(IConfigureThisEndpoint specifier, BusConfiguration config)
        {
            if (specifier is AsA_Worker)
            {
                config.Transactions().Enable();
                config.EnableFeature<Features.Sagas>();
            }
            
            Type transportDefinitionType;
            if (TryGetTransportDefinitionType(specifier, out transportDefinitionType))
            {
                config.UseTransport(transportDefinitionType);
            }
        }

        static bool TryGetTransportDefinitionType(IConfigureThisEndpoint specifier, out Type transportDefinitionType)
        {
            var transportType= specifier.GetType()
                .GetInterfaces()
                .Where(x => x.IsGenericType)
                .SingleOrDefault(x => x.GetGenericTypeDefinition() == typeof(UsingTransport<>));
            if (transportType != null)
            {
                transportDefinitionType = transportType.GetGenericArguments().First();
                return true;
            }
            transportDefinitionType = null;
            return false;
        }
    }


}
namespace NServiceBus
{
    using System;
    using NServiceBus.DataBus;

    public class AzureDataBus : DataBusDefinition
    {
        protected override Type ProvidedByFeature()
        {
            return typeof(AzureDataBusPersistence);
        }
    }
}
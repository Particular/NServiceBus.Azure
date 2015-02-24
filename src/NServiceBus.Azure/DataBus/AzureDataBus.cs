namespace NServiceBus
{
    using System;
    using DataBus;

    public class AzureDataBus : DataBusDefinition
    {
        protected override Type ProvidedByFeature()
        {
            return typeof(AzureDataBusPersistence);
        }
    }
}
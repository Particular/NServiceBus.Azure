namespace NServiceBus
{
    using System;
    using DataBus;

    /// <summary>
    /// Defines Azure databus that can be used by NServiceBus
    /// </summary>
    public class AzureDataBus : DataBusDefinition
    {
        protected override Type ProvidedByFeature()
        {
            return typeof(AzureDataBusPersistence);
        }
    }
}
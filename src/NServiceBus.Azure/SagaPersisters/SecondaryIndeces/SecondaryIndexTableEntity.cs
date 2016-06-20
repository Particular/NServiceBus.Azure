namespace NServiceBus.SagaPersisters.Azure.SecondaryIndeces
{
    using System;
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// An entity holding information about the secondary index.
    /// </summary>
    public class SecondaryIndexTableEntity : TableEntity
    {
        public Guid SagaId { get; set; }

        public byte[] InitialSagaData { get; set; }
    }
}
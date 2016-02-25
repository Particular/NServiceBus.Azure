namespace NServiceBus.SagaPersisters.Azure.SecondaryIndeces
{
    using Microsoft.WindowsAzure.Storage.Table;

    public sealed class PartitionRowKeyTuple
    {
        public string PartitionKey { get; }
        public string RowKey { get; }

        public PartitionRowKeyTuple(string partitionKey, string rowKey)
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
        }

        public void Apply(ITableEntity entity)
        {
            entity.PartitionKey = PartitionKey;
            entity.RowKey = RowKey;
        }

        private bool Equals(PartitionRowKeyTuple other)
        {
            return string.Equals(PartitionKey, other.PartitionKey) && string.Equals(RowKey, other.RowKey);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != this.GetType())
            {
                return false;
            }
            return Equals((PartitionRowKeyTuple) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((PartitionKey?.GetHashCode() ?? 0)*397) ^ (RowKey?.GetHashCode() ?? 0);
            }
        }
    }
}
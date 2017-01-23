namespace NServiceBus.AzureStoragePersistence.Tests
{
    using Microsoft.WindowsAzure.Storage.Table;

    public static class Extensions
    {
        public static void DeleteAllEntities(this CloudTable table)
        {
            TableQuerySegment<DynamicTableEntity> segment = null;

            while (segment == null || segment.ContinuationToken != null)
            {
                segment = table.ExecuteQuerySegmented(new TableQuery().Take(100), segment?.ContinuationToken);
                foreach (var entity in segment.Results)
                {
                    table.Execute(TableOperation.Delete(entity));
                }
            }
        }

        public static int CountAllEntities(this CloudTable table)
        {
            TableQuerySegment<DynamicTableEntity> segment = null;

            var count = 0;
            while (segment == null || segment.ContinuationToken != null)
            {
                segment = table.ExecuteQuerySegmented(new TableQuery().Take(100), segment?.ContinuationToken);
                if (segment != null)
                {
                    count += segment.Results.Count;
                }
            }

            return count;
        }
    }
}
namespace NServiceBus.Azure
{
    using System.Linq;
    using Microsoft.WindowsAzure.Storage.Table;
    using Microsoft.WindowsAzure.Storage.Table.DataServices;

    public class ServiceContext : TableServiceContext
    {
        public ServiceContext(CloudTableClient client)
            : base(client)
        {
        }

        public static string TimeoutManagerDataTableName = "TimeoutManagerData";

        public IQueryable<TimeoutManagerDataEntity> TimeoutManagerData
        {
            get
            {
                return CreateQuery<TimeoutManagerDataEntity>(TimeoutManagerDataTableName);
            }
        }

        public static string TimeoutDataTableName = "TimeoutData";

        public IQueryable<TimeoutDataEntity> TimeoutData
        {
            get
            {
                return CreateQuery<TimeoutDataEntity>(TimeoutDataTableName);
            }
        }

    }
}
//namespace NServiceBus
//{
//    using System.Collections.Generic;
//    using Features;
//    using Persistence;

//    class AzureStoragePersistence : IConfigurePersistence<AzureStorage>
//    {
//        public void Enable(Configure config, List<Storage> storagesToEnable)
//        {
//            if (storagesToEnable.Contains(Storage.Sagas))
//                config.Settings.EnableFeatureByDefault<AzureStorageSagaPersistence>();

//            if (storagesToEnable.Contains(Storage.Timeouts))
//                config.Settings.EnableFeatureByDefault<AzureStorageTimeoutPersistence>();

//            if (storagesToEnable.Contains(Storage.Subscriptions))
//                config.Settings.EnableFeatureByDefault<AzureStorageSubscriptionPersistence>();
//        }
//    }
//}
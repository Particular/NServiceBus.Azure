# Important Notice

NServiceBus.Azure repository is changing. There will be new repositories per Nuget package. All new development will be done in the new repositories only. This repository will be kept solely for support purposes.

# Repositories & NuGet packages
1. https://github.com/Particular/NServiceBus.AzureStorageQueues → NServiceBus.Azure.Transports.WindowsAzureStorageQueues
2. https://github.com/Particular/NServiceBus.AzureServiceBus → NServiceBus.Azure.Transports.WindowsAzureServiceBus
3. https://github.com/Particular/NServiceBus.Host.AzureCloudService → NServiceBus.Hosting.Azure, NServiceBus.Hosting.Azure.HostProcess
4. https://github.com/Particular/NServiceBus.Persistence.AzureStorage → NServiceBus.Persistence.AzureStorage
5. https://github.com/Particular/NServiceBus.DataBus.AzureBlobStorage → NServiceBus.DataBus.AzureBlobStorage

The Windows Azure transports for NServiceBus enable the use of Windows Azure Queues and Windows Azure Service Bus as the underlying transports used by NServiceBus. 

## Documentation

- [Azure Service Bus Transport](http://docs.particular.net/nservicebus/azure-servicebus/)
- [Azure Storage Queues Transport](http://docs.particular.net/nservicebus/azure-storage-queues/)
- [Azure Storage Persistence](https://github.com/Particular/NServiceBus.Persistence.AzureStorage)
- [Azure Cloud Services Host](http://docs.particular.net/nservicebus/hosting/cloudservices-host/)
- [Samples](http://docs.particular.net/samples/azure/)

## Third Party Dependency Update Policy

The azure related nuget packages depend on third party packages. As each of these packages is maintained by different parties in different ways, our update policy regarding them also varies.

The following aspects are taken into account:

- Third party follows SemVer
- API surface size
- Past experience with behavioral changes in the dependency


| Dependency                   | Current Policy                   |  Suggested Future Policy          | 
| ---------------------------- |---------------------------------:| ---------------------------------:|
| WindowsAzure.ServiceBus      | Fixed Major                      |  Fixed Major                      |
| WindowsAzure.Storage         | Fixed Major & Closed Major Range |  Closed Major Range               |
| Newtonsoft.Json              | Fixed Major                      |  Open Major Range                 |
| Microsoft.ServiceFabric.Data | Closed Minor Range               |  Closed Minor Range               |
---
title: Using Azure Storage Persistence In NServiceBus
summary: Using Windows Azure Storage for persistence features of NServiceBus including timeouts, sagas, and subscription storage.
originalUrl: http://docs.particular.net/articles/using-azure-storage-persistence-in-nservicebus
tags: []
---

Various features of NServiceBus require persistence. Among them are timeouts, sagas, and subscription storage. Various storage options are available including, Windows Azure Storage Services.

How To enable persistence with windows azure storage services
-----------------

First you need to reference the assembly that contains the azure storage persisters. The recommended way of doing this is by adding a nuget package reference to the  `NServiceBus.Azure` package to your project.

If self hosting, you can configure the persistence technology using the fluent configuration API and the extension methods found in the `NServiceBus.Azure` assembly


```C#

        static void Main()
        {
            Configure.Transactions.Enable();
            Configure.With()
            ...
				.AzureSubscriptionStorage()
				.AzureSagaPersister()
				.UseAzureTimeoutPersister()
			...
            .Start();
        }

```

When hosting in the Windows azure role entrypoint provided by `NServiceBus.Hosting.Azure`, these persistence strategies will be enabled by default.

But when hosting in a different NServiceBus provided host, you can enable them by implementing `INeedInitialization`, like this:


```C#

    public class EnableStorage : INeedInitialization
    {
        public void Init()
        {
            Configure.Instance
                .AzureSubscriptionStorage()
                .AzureSagaPersister()
                .UseAzureTimeoutPersister();
        }
    }

```

Detailed configuration
----------------------

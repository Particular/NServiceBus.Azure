---
title: Hosting NServiceBus in Windows Azure
summary: Using Windows Azure Cloudservices, Websites and virtual machines to host NServiceBus.
originalUrl: http://docs.particular.net/articles/hosting-nservicebus-in-windows-azure
tags: []
---

The Windows Azure Platform and NServiceBus make a perfect fit. On the one hand the azure platform offers us the scalable and flexible platform that we are looking for in our designs, on the other hand NServiceBus makes development on this highly distributed environment a breeze.

Windows Azure offers various ways to host applications, each of these hosting options can be used in context of NServicebus, but there are some things to keep in mind for each of them.

General Consideration
-----------------

The windows azure platform consists of a number of huge datacenters, and with huge I do mean huge... hundreds of thousands of machines each.

One of the side effects of datacenters this size, is that some of the technologies that we take for granted in smaller networks, may not work the way you assume anymore. 

One of those technologies is the well known 2 phase commit protocol, better known as the DTC, or Distributed Transaction Coordinator in windows. In the 2PC protocol each participant in the transaction communication needs to confirm success of the transaction twice. In large systems, this protocol becomes extremely slow as the amount of network traffic grows exponentially with the size of the network, and often the protocol doesn't complete at all because there is some form network partitioning. As an example, doing a single transaction on a 1000 node system, would involve 2000 network operations to make the protocol complete and one faulty router port can make it go in doubt and never complete. Imagine doing millions per second across hundreds of thousands of nodes as they do in azure...

Windows Azure Virtual Machines
-----------------

The Virtual Machines hosting model, is much the same as any other virtualization technology that you may have in your own datacenter. Machines are created from a virtual machine template, you are responsible for managing their content and any change you make to them is persisted in some backend storage.

The installation model is therefore also the same as any on premise nservicebus project, use `NServiceBus.Host.exe` to run your endpoint, or use the `Configure` api to self host your endpoint in for example a website.

The main difference, as outlined above, is that you should not rely on any technology that itself depends on 2PC. In other words, Msmq is not a good transport in this environment, it's better to go with `AzureStorageQueues` or `AzureServiceBus` instead, other options include deploying `RabbitMQ` or other non-DTC transport to an azure Virtual Machine and go with that.


Windows Azure Websites
-----------------

Another interesting deployment model is called Windows Azure Websites. In this deployment model, you simply create a regular website, push it to your favorite supported source control repository (like github), and Microsoft will take it from there. They will get the latest version, build the binaries, run your tests and deploy to production on your behalf.

As from an NServiceBus programming model, this is roughly the same as any other self hosted endpoint in a website, you use the `Configure` api to set things up and it will work.

The only quirk in this model though, is that Windows Azure Websites has been built with 'cheap hosting' in mind. It comes with technology that will put your website in a suspended mode when there is no traffic. This also implies that if you have an NServiceBus endpoint hosted here, it will also become suspended (and there is nothing you can do about it). Therefore, it is advised to only use azure websites as sendonly endpoints, or figure out a way to keep traffic coming.


Cloud Services 
-----------------




Cloud Services - Worker Roles
-----------------





First you need to reference the assembly that contains the azure storage persisters. The recommended way of doing this is by adding a nuget package reference to the `NServiceBus.Azure` package to your project.

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

You can get more control on the behavior of each persister by specifying one of the respective configuration sections in your app.config and changing one of the available properties.

```XML

	  <configSections>
	    <section name="AzureSubscriptionStorageConfig" type="NServiceBus.Config.AzureSubscriptionStorageConfig, NServiceBus.Azure" />
	    <section name="AzureSagaPersisterConfig" type="NServiceBus.Config.AzureSagaPersisterConfig, NserviceBus.Azure" />
	    <section name="AzureTimeoutPersisterConfig" type="NServiceBus.Config.AzureTimeoutPersisterConfig, NserviceBus.Azure" />
	  </configSections>

	<AzureSagaPersisterConfig ConnectionString="UseDevelopmentStorage=true" />
  	<AzureTimeoutPersisterConfig ConnectionString="UseDevelopmentStorage=true" />
  	<AzureSubscriptionStorageConfig ConnectionString="UseDevelopmentStorage=true" />

```

The following settings are available for changing the behavior of subscription persistence through the `AzureSubscriptionStorageConfig` section:

- `ConnectionString`: Allows you to set the connectionstring to the storage account for storing subscription information, defaults to `UseDevelopmentStorage=true`
- `CreateSchema`: Instructs the persister to create the table automatically, defaults to true
- `TableName`: Lets you choose the name of the table for storing subscriptions, defaults to `Subscription`.


The following settings are available for changing the behavior of saga persistence through the `AzureSagaPersisterConfig`section:

- `ConnectionString`: Allows you to set the connectionstring to the storage account for storing saga information, defaults to `UseDevelopmentStorage=true`
- `CreateSchema`: Instructs the persister to create the table automatically, defaults to true


The following settings are available for changing the behavior of timeout persistence through the `AzureTimeoutPersisterConfig` section:

- `ConnectionString`: Allows you to set the connectionstring to the storage account for storing tiemout information, defaults to `UseDevelopmentStorage=true`
- `TimeoutManagerDataTableName`: Allows you to set the name of the table where the timeout manager stores it's internal state, defaults to `TimeoutManagerDataTable`
- `TimeoutDataTableName`: Allows you to set the name of the table where the timeouts themselves are stored, defaults to `TimeoutDataTableName`
- `CatchUpInterval`: When a node hosting a timeout manager would go down, it needs to catch up with missed timeouts faster than it normally would (1 sec), this value allows you to set the catchup interval in seconds. Defaults to 3600, meaning it will process one hour at a time.
- `PartitionKeyScope`: The time range used as partitionkey value for all timeouts. For optimal performance, this should be in line with the catchup interval so it should come to no surprise that the default value also represents an hour: yyyMMddHH. 

Sample
------

Want to see these persisters in action? Checkout the [Video store sample.](https://github.com/Particular/NServiceBus.Azure.Samples/tree/master/VideoStore.AzureStorageQueues.Cloud) and more specifically, the `VideoStore.Sales` endpoint























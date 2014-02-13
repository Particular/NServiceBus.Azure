---
title: Understanding transactions in Windows Azure
summary: Understanding what kind of transactions are supported in Windows Azure and how we deal with this in NServiceBus.
originalUrl: http://docs.particular.net/articles/hosting-nservicebus-in-windows-azure
tags: []
---

The Windows Azure Platform and NServiceBus make a perfect fit. On the one hand the azure platform offers us the scalable and flexible platform that we are looking for in our designs, and on the other hand it is NServiceBus that makes development on this highly distributed environment a breeze. However, there are a few things to keep in mind when developing for this platform, the most important being, the lack of transactions in windows azure.

The windows azure platform consists of a number of huge datacenters, and with huge I do mean huge... hundreds of thousands of machines each. One of the side effects of datacenters this size, is that some of the technologies that we take for granted in smaller networks, may not work the way you assume anymore. And one of those is the concept of a transaction

So, let's start with a bold statement to get you thinking: **There are no transactions in windows azure!** (The real statement is obviously a bit more nuanced)

No distributed transactions
---------------------------

Distributed transactions, better known as the DTC, or Distributed Transaction Coordinator in windows, rely on the 2 phase commit protocol to operate internally. In the 2PC protocol each participant in the transaction communication needs to confirm success of the transaction twice before the overall transaction is accepted. In large systems, this protocol becomes extremely slow as the amount of network traffic grows exponentially with the size of the network, and often the protocol doesn't complete at all because there is some form network partitioning at any given point in time. As an example, doing a single transaction on a 1000 node system, would involve 2000 network operations to make the protocol complete and one faulty router can make it go in doubt and never complete. Imagine doing millions per second across hundreds of thousands of nodes as they do in azure...

Local transactions
---------------------------



### Windows Azure Storage
### Windows Azure Servicebus
### Windows Azure Database



























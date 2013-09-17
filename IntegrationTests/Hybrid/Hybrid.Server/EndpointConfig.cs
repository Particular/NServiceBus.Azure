namespace Hybrid.Server
{
    using NServiceBus;

	/*
		This class configures this endpoint as a Client. More information about how to configure the NServiceBus host
		can be found here: http://particular.net/articles/the-nservicebus-host
	*/
	public class EndpointConfig : IConfigureThisEndpoint, AsA_Server
    {
    }
}
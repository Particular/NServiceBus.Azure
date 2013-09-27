namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{
    /// <summary>
    /// 
    /// </summary>
    public interface ICreateTopics
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        string Create(Address address);
    }
}
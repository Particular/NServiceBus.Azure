namespace NServiceBus.Transports
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
        void CreateIfNecessary(Address address);
    }
}
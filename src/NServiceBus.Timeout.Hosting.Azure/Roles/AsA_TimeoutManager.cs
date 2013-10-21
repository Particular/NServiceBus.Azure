using NServiceBus.Hosting.Roles;

namespace NServiceBus.Timeout.Hosting.Azure
{
    /// <summary>
    /// The entire endpoint acts as a timeoutmanager 
    /// </summary>
    [ObsoleteEx(RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.0", Replacement = "Use the satelite instead")]        
    public interface AsA_TimeoutManager : IRole{}
}

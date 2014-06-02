using NServiceBus.Hosting.Roles;
using NServiceBus.Unicast.Config;

namespace NServiceBus.Hosting.Azure.Roles.Handlers
{
    /// <summary>
    /// Handles configuration related to the server role
    /// </summary>
    public class WorkerRoleHandler : IConfigureRole<AsA_Worker>
    {
        public void ConfigureRole(IConfigureThisEndpoint specifier, Configure config)
        {
            config.Transactions(t => t.Enable());
            config.Features(f => f.Enable<Features.Sagas>());
        }
    }
}
                    
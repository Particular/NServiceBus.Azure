using NServiceBus.Hosting.Roles;

namespace NServiceBus.Hosting.Azure.Roles.Handlers
{
    /// <summary>
    /// Handles configuration related to the server role
    /// </summary>
    class WorkerRoleHandler : IConfigureRole<AsA_Worker>
    {
        public void ConfigureRole(IConfigureThisEndpoint specifier, Configure config)
        {
            config.Transactions(t => t.Enable());
            config.EnableFeature<Features.Sagas>();
        }
    }
}
                    
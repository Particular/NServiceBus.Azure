
namespace VideoStore.ContentManagement
{
    using Microsoft.WindowsAzure.ServiceRuntime;
    using NServiceBus.Hosting.Azure;

    // Note: We're using NServiceBus.Hosting.Azure.RoleEntryPoint as a base class

    public class WorkerRole : RoleEntryPoint
    {
        private readonly NServiceBusRoleEntrypoint nsb = new NServiceBusRoleEntrypoint();

        public override bool OnStart()
        {
            nsb.Start();

            return base.OnStart();
        }

        public override void OnStop()
        {
            nsb.Stop();

            base.OnStop();
        }
    }
}
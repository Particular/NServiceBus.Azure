namespace NServiceBus.Config
{
    using System.IO;
    using Microsoft.WindowsAzure.ServiceRuntime;

    public class SafeRoleEnvironment
    {
        public static bool IsAvailable {
            get
            {
                try
                {
                    return RoleEnvironment.IsAvailable;
                }
                catch (FileNotFoundException ex)
                {
                    if (ex.Message.Contains("msshrtmi"))
                    {
                        return false;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }
    }
}
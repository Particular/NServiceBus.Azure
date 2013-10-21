namespace NServiceBus.Config
{
    using System;
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
                catch (TypeInitializationException ex)
                {
                    var e = ex.InnerException;
                    if (e is FileNotFoundException && e.Message.Contains("msshrtmi"))
                    {
                        return false;
                    }
                    throw;
                }
            }
        }
    }
}
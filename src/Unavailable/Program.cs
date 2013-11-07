using System;

namespace Unavailable
{
    using NServiceBus.Config;

    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (SafeRoleEnvironment.IsAvailable)
                {
                    var instanceId = SafeRoleEnvironment.CurrentRoleInstanceId;
                    var deploymentId = SafeRoleEnvironment.DeploymentId;
                    var roleName = SafeRoleEnvironment.CurrentRoleName;
                    var connectionString1 = SafeRoleEnvironment.GetConfigurationSettingValue("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString");
                    string connectionString2;
                    SafeRoleEnvironment.TryGetConfigurationSettingValue(
                        "Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString", out connectionString2);

                    string doesnotexist;
                    SafeRoleEnvironment.TryGetConfigurationSettingValue(
                        "This.Does.Not.Exist", out doesnotexist);


                    var path1 = SafeRoleEnvironment.GetRootPath("endpoints");
                    string path2;
                    SafeRoleEnvironment.TryGetRootPath("endpoints", out path2);
                    string path3;
                    SafeRoleEnvironment.TryGetRootPath("what", out path3);

                    Console.WriteLine(instanceId);
                    Console.WriteLine(deploymentId);
                    Console.WriteLine(roleName);
                    Console.WriteLine(connectionString1);
                    Console.WriteLine(connectionString2);
                    Console.WriteLine(doesnotexist);
                    Console.WriteLine(path1);
                    Console.WriteLine(path2);
                    Console.WriteLine(path3);

                    //SafeRoleEnvironment.RequestRecycle();
                }
            }
            catch (Exception ex)
            {
                var inner = ex;

                while (inner != null)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);

                    inner = inner.InnerException;
                }
                
            }
           

            Console.ReadLine();
        }
    }
}

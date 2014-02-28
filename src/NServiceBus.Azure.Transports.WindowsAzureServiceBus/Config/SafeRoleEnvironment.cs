namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;

    [DebuggerNonUserCode] 
    public static class SafeRoleEnvironment
    {
        static bool isAvailable = true;
        static Type roleEnvironmentType;
        static Type roleInstanceType;
        static Type roleType;
        static Type localResourceType;
       
        static SafeRoleEnvironment()
        {
            try
            {
                TryLoadRoleEnvironment();
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
                throw;
            }
            
        }

        public static bool IsAvailable {
            get
            {
                return isAvailable;
            }
        }

        public static string CurrentRoleInstanceId {
            get
            {
                if (!IsAvailable) throw new RoleEnvironmentUnavailableException("Role environment is not available, please check IsAvailable before calling this property!");

                var instance = roleEnvironmentType.GetProperty("CurrentRoleInstance").GetValue(null, null);
                return (string) roleInstanceType.GetProperty("Id").GetValue(instance, null);
            } 
        }

        public static string DeploymentId
        {
            get
            {
                if (!IsAvailable) throw new RoleEnvironmentUnavailableException("Role environment is not available, please check IsAvailable before calling this property!");

                return (string) roleEnvironmentType.GetProperty("DeploymentId").GetValue(null, null);
            }
        }
        public static string CurrentRoleName
        {
            get
            {
                if (!IsAvailable) throw new RoleEnvironmentUnavailableException("Role environment is not available, please check IsAvailable before calling this property!");

                var instance = roleEnvironmentType.GetProperty("CurrentRoleInstance").GetValue(null, null);
                var role = roleInstanceType.GetProperty("Role").GetValue(instance, null);
                return (string) roleType.GetProperty("Name").GetValue(role, null);
            }
        }

        public static string GetConfigurationSettingValue(string name)
        {
            if (!IsAvailable) throw new RoleEnvironmentUnavailableException("Role environment is not available, please check IsAvailable before calling this method!");

            return (string) roleEnvironmentType.GetMethod("GetConfigurationSettingValue").Invoke(null, new object[] { name });
        }

        public static bool TryGetConfigurationSettingValue(string name, out string setting)
        {
            if (!IsAvailable) throw new RoleEnvironmentUnavailableException("Role environment is not available, please check IsAvailable before calling this method!");

            setting = string.Empty;
            var result = false;
            try
            {
                setting = (string)roleEnvironmentType.GetMethod("GetConfigurationSettingValue").Invoke(null, new object[] { name });
                result = !string.IsNullOrEmpty(setting);
            }
            catch (Exception ex)
            {
                var inner = ex;

                while (inner != null)
                {
                    if (inner.GetType().Name.Contains("RoleEnvironmentException"))
                        return result;

                    inner = inner.InnerException;
                }

                throw;
            }

            return result;
        }

        public static void RequestRecycle()
        {
            if (!IsAvailable) throw new RoleEnvironmentUnavailableException("Role environment is not available, please check IsAvailable before calling this method!");

            roleEnvironmentType.GetMethod("RequestRecycle").Invoke(null, null);
        }

        public static string GetRootPath(string name)
        {
            if (!IsAvailable) throw new RoleEnvironmentUnavailableException("Role environment is not available, please check IsAvailable before calling this method!");

            var o = roleEnvironmentType.GetMethod("GetLocalResource").Invoke(null, new object[] { name });
            return (string)localResourceType.GetProperty("RootPath").GetValue(o, null);
            }

        public static bool TryGetRootPath(string name, out string path)
        {
            if (!IsAvailable) throw new RoleEnvironmentUnavailableException("Role environment is not available, please check IsAvailable before calling this method!");

            var result = false;
            path = string.Empty;

            try
            {
                path = GetRootPath(name);
                result = path != null;
            }
            catch (Exception ex)
            {
                var inner = ex;

                while (inner != null)
                {
                    if (inner.GetType().Name.Contains("RoleEnvironmentException"))
                        return result;

                    inner = inner.InnerException;
                }

                throw;
            }

            return result;
        }

        static void TryLoadRoleEnvironment()
        {
            var serviceRuntimeAssembly = TryLoadServiceRuntimeAssembly();
            if (!isAvailable) return;

            TryGetRoleEnvironmentTypes(serviceRuntimeAssembly);
            if (!isAvailable) return;
            
            isAvailable = IsAvailableInternal();

        }

        static void TryGetRoleEnvironmentTypes(Assembly serviceRuntimeAssembly)
        {
            try
            {
                roleEnvironmentType = serviceRuntimeAssembly.GetType("Microsoft.WindowsAzure.ServiceRuntime.RoleEnvironment");
                roleInstanceType = serviceRuntimeAssembly.GetType("Microsoft.WindowsAzure.ServiceRuntime.RoleInstance");
                roleType = serviceRuntimeAssembly.GetType("Microsoft.WindowsAzure.ServiceRuntime.Role");
                localResourceType = serviceRuntimeAssembly.GetType("Microsoft.WindowsAzure.ServiceRuntime.LocalResource");
            }
            catch (ReflectionTypeLoadException)
            {
                isAvailable = false;
            }
        }

        static bool IsAvailableInternal()
        {
            try
            {
                return (bool)roleEnvironmentType.GetProperty("IsAvailable").GetValue(null, null);
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

        static Assembly TryLoadServiceRuntimeAssembly()
        {
            try
            {
                var ass = Assembly.LoadWithPartialName("Microsoft.WindowsAzure.ServiceRuntime");
                isAvailable = ass != null;
                return ass;
            }
            catch (FileNotFoundException)
            {
                isAvailable = false;
                return null;
            }
        }
    }
}
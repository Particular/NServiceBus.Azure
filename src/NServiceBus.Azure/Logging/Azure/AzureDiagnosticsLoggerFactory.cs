using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.Diagnostics.Management;
using Microsoft.WindowsAzure.ServiceRuntime;
using NServiceBus.Logging;

namespace NServiceBus.Integration.Azure
{
    using System.Security;
    using Config.ConfigurationSource;
    using NServiceBus.Azure;

    /// <summary>
    /// 
    /// </summary>
    public class AzureDiagnosticsLoggerFactory : ILoggerFactory
    {
        const string Prefix = "Microsoft.WindowsAzure.Plugins.Diagnostics";
        private const string ConnectionStringKey = "ConnectionString";
        private const string LevelKey = "Level";
        private const string ScheduledTransferPeriodKey = "ScheduledTransferPeriod";
        private const string EventLogsKey = "EventLogs";

        IConfigurationSource internalConfigurationSource;

        public AzureDiagnosticsLoggerFactory()
        {
            internalConfigurationSource = new AzureConfigurationSource(new AzureConfigurationSettings())
            {
                ConfigurationPrefix = Prefix
            };
        }

        public int ScheduledTransferPeriod { get; set; }

        public string Level { get; set; }

        public bool InitializeDiagnostics { get; set; }

        public bool Enable { get; set; }

        public ILog GetLogger(Type type)
        {
            var logger = new AzureDiagnosticsLogger();
            logger.Level = GetLevel();
            return logger;
        }

        public ILog GetLogger(string name)
        {
            var logger = new AzureDiagnosticsLogger();
            logger.Level = GetLevel();
            return logger;
        }

        public void ConfigureAzureDiagnostics()
        {
            if (Enable)
            {
                var exists = Trace.Listeners.Cast<TraceListener>().Count(tracelistener => tracelistener.GetType().IsAssignableFrom(typeof(DiagnosticMonitorTraceListener))) > 0;
                if (!exists)
                {
                    try
                    {
                        var listener = new DiagnosticMonitorTraceListener();
                        Trace.Listeners.Add(listener);
                    }
                    catch (SecurityException)
                    {
                        return;
                    }
                    catch (InvalidOperationException)
                    {
                        return;
                    }
                }
            }
            else
            {
                var exists = Trace.Listeners.Cast<TraceListener>().Count(tracelistener => tracelistener.GetType().IsAssignableFrom(typeof(ConsoleTraceListener))) > 0;
                if (!exists) Trace.Listeners.Add(new ConsoleTraceListener());
            }

            if (!RoleEnvironment.IsAvailable || !InitializeDiagnostics) return;

            var roleInstanceDiagnosticManager = CloudAccountDiagnosticMonitorExtensions.CreateRoleInstanceDiagnosticManager(
                GetConnectionString(),
                RoleEnvironment.DeploymentId,
                RoleEnvironment.CurrentRoleInstance.Role.Name,
                RoleEnvironment.CurrentRoleInstance.Id);

            var configuration = roleInstanceDiagnosticManager.GetCurrentConfiguration();

            if (configuration == null) 
            {
                configuration = DiagnosticMonitor.GetDefaultInitialConfiguration();
                
                ConfigureDiagnostics(configuration);

                DiagnosticMonitor.Start(Prefix + "." + ConnectionStringKey, configuration);
            }
           
        }

        private void ConfigureDiagnostics(DiagnosticMonitorConfiguration configuration)
        {
            configuration.Logs.ScheduledTransferLogLevelFilter = GetLevel();

            ScheduleTransfer(configuration);

            ConfigureWindowsEventLogsToBeTransferred(configuration);
        }

        private void ScheduleTransfer(DiagnosticMonitorConfiguration dmc)
        {
            ScheduledTransferPeriod = GetScheduledTransferPeriod();
            var transferPeriod = TimeSpan.FromMinutes(ScheduledTransferPeriod);
            dmc.Logs.ScheduledTransferPeriod = transferPeriod;
            dmc.WindowsEventLog.ScheduledTransferPeriod = transferPeriod;
        }

        private void ConfigureWindowsEventLogsToBeTransferred(DiagnosticMonitorConfiguration dmc)
        {
            var eventLogs = GetEventLogs().Split(';');
            foreach (var log in eventLogs)
            {
                dmc.WindowsEventLog.DataSources.Add(log);
            }
        }

        private string GetConnectionString()
        {
            return internalConfigurationSource.GetConfiguration<Diagnostics>().ConnectionString;
        }

        private LogLevel GetLevel()
        {
            return (LogLevel)Enum.Parse(typeof(LogLevel), internalConfigurationSource.GetConfiguration<Diagnostics>().Level);
        }

        private int GetScheduledTransferPeriod()
        {
            return internalConfigurationSource.GetConfiguration<Diagnostics>().ScheduledTransferPeriod;
        }

        private string GetEventLogs()
        {
            return internalConfigurationSource.GetConfiguration<Diagnostics>().EventLogs;
        }
    }
}

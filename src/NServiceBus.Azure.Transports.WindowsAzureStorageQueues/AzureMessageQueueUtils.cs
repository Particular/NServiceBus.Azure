﻿namespace NServiceBus.Azure.Transports.WindowsAzureStorageQueues
{
    using System;
    using System.Configuration;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Helper class 
    /// </summary>
    public class AzureMessageQueueUtils
    {

        public static string GetQueueName(Address address)
        {
            // The auto queue name generation uses namespaces which includes dots, 
            // yet dots are not supported in azure storage names
            // that's why we replace them here.

            var name = address.Queue.Replace('.', '-').ToLowerInvariant();

            if (name.Length > 63)
            {
                var nameGuid = DeterministicGuidBuilder(name).ToString();
                name = name.Substring(0, 63 - nameGuid.Length - 1).Trim('-') + "-" + nameGuid;
            }

            if (! IsValidQueueName(name))
            {
                throw new ConfigurationErrorsException(string.Format("Invalid Queuename {0}, rules for naming queues can be found at http://msdn.microsoft.com/en-us/library/windowsazure/dd179349.aspx", name));
            }

            return name;
        }

        public static bool IsValidQueueName(string name)
        {
            return new Regex(@"^(?=.{3,63}$)[a-z0-9](-?[a-z0-9])+$").IsMatch(name);
        }

        static Guid DeterministicGuidBuilder(string input)
        {
            //use MD5 hash to get a 16-byte hash of the string
            using (var provider = new MD5CryptoServiceProvider())
            {
                byte[] inputBytes = Encoding.Default.GetBytes(input);
                byte[] hashBytes = provider.ComputeHash(inputBytes);
                //generate a guid from the hash:
                return new Guid(hashBytes);
            }
        }
    }
}
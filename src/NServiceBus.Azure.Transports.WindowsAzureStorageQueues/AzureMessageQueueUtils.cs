namespace NServiceBus.Azure.Transports.WindowsAzureStorageQueues
{
    using System;
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
            var name = SanitizeQueueName(address.Queue.ToLowerInvariant());
            var guidinput = address.Queue.Replace('.', '-').ToLowerInvariant(); // this string was used in the past to calculate guid, should stay backward compat
                
            if (name.Length > 63)
            {
                var nameGuid = DeterministicGuidBuilder(guidinput).ToString();
                name = name.Substring(0, 63 - nameGuid.Length - 1).Trim('-') + "-" + nameGuid;
            }
            
            return name;
        }

        static string SanitizeQueueName(string queueName)
        {
            //rules for naming queues can be found at http://msdn.microsoft.com/en-us/library/windowsazure/dd179349.aspx"

            var rgx = new Regex(@"[^a-zA-Z0-9\-]");
            var n = rgx.Replace(queueName, "-"); // this can lead to multiple - occurences in a row
            var oc = new Regex(@"\-+");
            n = oc.Replace(n, "-");
            return n;
        }

        static Guid DeterministicGuidBuilder(string input)
        {
            //use MD5 hash to get a 16-byte hash of the string
            using (var provider = new MD5CryptoServiceProvider())
            {
                var inputBytes = Encoding.Default.GetBytes(input);
                var hashBytes = provider.ComputeHash(inputBytes);
                //generate a guid from the hash:
                return new Guid(hashBytes);
            }
        }
    }
}
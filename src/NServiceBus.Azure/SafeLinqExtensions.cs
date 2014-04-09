namespace NServiceBus.Azure
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.WindowsAzure.Storage;

    public static class SafeLinqExtensions
    {
        public static TSource SafeFirstOrDefault<TSource>(this IEnumerable<TSource> source)
        {
            try
            {
                return source.FirstOrDefault();
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation.HttpStatusCode == 404) return default(TSource);

                throw;
            }
        }
    }
}
namespace NServiceBus
{
    using System.Net;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;

    static class CloudTableExtensions
    {
        /// <summary>
        /// Safely deletes an entitym ignoring not found exception.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static void DeleteIgnoringNotFound(this CloudTable table, ITableEntity entity)
        {
            try
            {
                table.Execute(TableOperation.Delete(entity));
            }
            catch (StorageException ex)
            {
                // Horrible logic to check if item has already been deleted or not
                var webException = ex.InnerException as WebException;
                if (webException?.Response != null)
                {
                    var response = (HttpWebResponse)webException.Response;
                    if ((int)response.StatusCode != 404)
                    {
                        // Was not a previously deleted exception, throw again
                        throw;
                    }
                }
            }
        }
    }
}
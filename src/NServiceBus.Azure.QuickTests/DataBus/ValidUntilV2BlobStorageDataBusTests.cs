using System;
using Microsoft.WindowsAzure.Storage.Blob;
using NUnit.Framework;

[TestFixture]
class ValidUntilV2BlobStorageDataBusTests : ValidUntilTest
{
    
    //https://github.com/Particular/NServiceBus.Azure/blob/e9db29beb21d1fd914191e479cb5948fffd92f3b/src/NServiceBus.Azure/DataBus/Azure/BlobStorage/BlobStorageDataBus.cs#L41
    protected override void SetValidUntil(ICloudBlob cloudBlob, TimeSpan timeToBeReceived)
    {
        if (timeToBeReceived == TimeSpan.MaxValue)
        {
            cloudBlob.Metadata["ValidUntil"] = TimeSpan.MaxValue.ToString();
        }
        else
        {
            cloudBlob.Metadata["ValidUntil"] = (DateTime.Now + timeToBeReceived).ToString();
        }
    }

}
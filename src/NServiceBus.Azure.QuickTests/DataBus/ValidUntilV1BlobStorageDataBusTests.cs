using System;
using Microsoft.WindowsAzure.Storage.Blob;
using NUnit.Framework;

[TestFixture]
class ValidUntilV1BlobStorageDataBusTests : ValidUntilTest
{

    //https://github.com/Particular/NServiceBus.Azure/blob/ba6b0de53255072764f9aaf433c6487e12bc41ed/src/impl/databus/NServiceBus.DataBus.Azure.BlobStorage/BlobStorageDataBus.cs#L46
    protected override void SetValidUntil(ICloudBlob cloudBlob, TimeSpan timeToBeReceived)
    {
        cloudBlob.Metadata["ValidUntil"] = (DateTime.Now + timeToBeReceived).ToString();
    }

    [Explicit("this never worked since TimeSpan.MaxValue would overflow the datetime math")]
    public override void ValidUntil_defaults_to_DateTimeMax()
    {
        
    }
}
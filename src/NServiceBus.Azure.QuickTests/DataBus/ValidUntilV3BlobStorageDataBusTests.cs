using System;
using Microsoft.WindowsAzure.Storage.Blob;
using NUnit.Framework;

[TestFixture]
class ValidUntilV3BlobStorageDataBusTests : ValidUntilTest
{
    //https://github.com/Particular/NServiceBus.Azure/blob/21ab8ab2e6fc413c4113b098c4156c64b48f860e/src/NServiceBus.Azure/DataBus/Azure/BlobStorage/BlobStorageDataBus.cs#L42
    protected override void SetValidUntil(ICloudBlob cloudBlob, TimeSpan timeToBeReceived)
    {
        if (timeToBeReceived == TimeSpan.MaxValue)
        {
            cloudBlob.Metadata["ValidUntil"] = TimeSpan.MaxValue.ToString();
        }
        else
        {
            cloudBlob.Metadata["ValidUntil"] = (DateTime.UtcNow + timeToBeReceived).ToString();
        }
        cloudBlob.Metadata["ValidUntilKind"] = "Utc";
    }

    [Ignore("no way this can work since we cannot be sure what culture the value was writen in")]
    public override void ValidUntil_is_not_corrupt_by_change_in_local()
    {
        base.ValidUntil_is_not_corrupt_by_change_in_local();
    }
}
using System;
using Microsoft.WindowsAzure.Storage.Blob;
using NServiceBus.DataBus.Azure.BlobStorage;
using NUnit.Framework;

[TestFixture]
class ValidUntilV4BlobStorageDataBusTests : ValidUntilTest
{
    protected override void SetValidUntil(ICloudBlob cloudBlob, TimeSpan timeToBeReceived)
    {
        BlobStorageDataBus.SetValidUntil(cloudBlob, timeToBeReceived);
    }

    [Test]
    public void ValidUntilKind_defaults_to_Utc()
    {
        var cloudBlob = StubACloudBlob();

        SetValidUntil(cloudBlob, TimeSpan.FromHours(1));
        Assert.AreEqual("Utc", cloudBlob.Metadata["ValidUntilKind"]);
    }
}
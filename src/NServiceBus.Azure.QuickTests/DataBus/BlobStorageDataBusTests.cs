using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Microsoft.WindowsAzure.Storage.Blob;
using NServiceBus.DataBus.Azure.BlobStorage;
using NUnit.Framework;
using Rhino.Mocks;

[TestFixture]
class BlobStorageDataBusTests
{

    [Test]
    public void ValidUntil_is_correctly_set()
    {
        var cloudBlob = StubACloudBlob();

        BlobStorageDataBus.SetValidUntil(cloudBlob, TimeSpan.FromHours(1));
        var resultValidUntil = BlobStorageDataBus.GetValidUntil(cloudBlob);

        Assert.That(resultValidUntil, Is.EqualTo(DateTime.UtcNow.AddHours(1))
            .Within(TimeSpan.FromSeconds(10)));
    }

    [Test]
    public void ValidUntil_is_not_corrupt_by_change_in_local()
    {
        var currentCulture = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            var cloudBlob = StubACloudBlob();
            var dateTime = new DateTime(2100, 1, 4, 12, 0, 0);
            var timeSpan = dateTime - DateTime.UtcNow;
            BlobStorageDataBus.SetValidUntil(cloudBlob, timeSpan);
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-AU");
            var resultValidUntil = BlobStorageDataBus.GetValidUntil(cloudBlob);
            Assert.AreEqual(4, resultValidUntil.Day);
            Assert.AreEqual(1, resultValidUntil.Month);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = currentCulture;
        }
    }

    [Test]
    public void ValidUntil_should_default_to_DateTimeMax_for_corrupted_value()
    {
        var cloudBlob = StubACloudBlob();
        BlobStorageDataBus.SetValidUntil(cloudBlob, TimeSpan.FromHours(1));
        //HACK: set ValidUntil to be a non parsable string
        cloudBlob.Metadata["ValidUntil"] = "Not a date time";
        var resultValidUntil = BlobStorageDataBus.GetValidUntil(cloudBlob);
        Assert.AreEqual(DateTime.MaxValue, resultValidUntil);
    }

    [Test]
    public void ValidUntil_is_UtcKind()
    {
        var cloudBlob = StubACloudBlob();
        BlobStorageDataBus.SetValidUntil(cloudBlob, TimeSpan.FromHours(1));
        var resultValidUntil = BlobStorageDataBus.GetValidUntil(cloudBlob);
        Assert.AreEqual(DateTimeKind.Utc, resultValidUntil.Kind);
    }

    [Test]
    public void ValidUntilKind_defaults_to_Utc()
    {
        var cloudBlob = StubACloudBlob();

        BlobStorageDataBus.SetValidUntil(cloudBlob, TimeSpan.FromHours(1));
        Assert.AreEqual("Utc", cloudBlob.Metadata["ValidUntilKind"]);
    }

    [Test]
    public void ValidUntil_defaults_to_DateTimeMax()
    {
        var cloudBlob = StubACloudBlob();

        BlobStorageDataBus.SetValidUntil(cloudBlob, TimeSpan.MaxValue);
        var resultValidUntil = BlobStorageDataBus.GetValidUntil(cloudBlob);
        Assert.AreEqual(DateTime.MaxValue, resultValidUntil);
    }

    [Test]
    public void Legacy_ValidUntil_is_correctly_set()
    {
        var cloudBlob = StubACloudBlob();

        //HACK: set ValidUntil to the non UTC legacy value
        cloudBlob.Metadata["ValidUntil"] = (DateTime.Now + TimeSpan.FromHours(1)).ToString();
        var resultValidUntil = BlobStorageDataBus.GetValidUntil(cloudBlob);
        Assert.That(resultValidUntil, Is.EqualTo(DateTime.UtcNow.AddHours(1))
            .Within(TimeSpan.FromSeconds(10)));
    }

    [Test]
    public void Legacy_ValidUntil_Still_returns_UtcKind()
    {
        var cloudBlob = StubACloudBlob();

        //HACK: set ValidUntil to the non UTC legacy value
        cloudBlob.Metadata["ValidUntil"] = (DateTime.Now + TimeSpan.FromHours(1)).ToString();
        var resultValidUntil = BlobStorageDataBus.GetValidUntil(cloudBlob);
        Assert.AreEqual(DateTimeKind.Utc, resultValidUntil.Kind);
    }

    static ICloudBlob StubACloudBlob()
    {
        var cloudBlob = MockRepository.GenerateStub<ICloudBlob>();
        cloudBlob.Stub(x => x.Metadata)
            .Return(new Dictionary<string, string>());
        cloudBlob.Stub(x => x.SetMetadata());
        return cloudBlob;
    }
}

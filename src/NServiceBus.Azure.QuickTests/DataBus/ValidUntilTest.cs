using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Microsoft.WindowsAzure.Storage.Blob;
using NServiceBus.DataBus.Azure.BlobStorage;
using NUnit.Framework;
using Rhino.Mocks;

abstract class ValidUntilTest
{
    [Test]
    public void ValidUntil_is_correctly_set()
    {
        var cloudBlob = StubACloudBlob();

        SetValidUntil(cloudBlob, TimeSpan.FromHours(1));
        var resultValidUntil = BlobStorageDataBus.GetValidUntil(cloudBlob);

        Assert.That(resultValidUntil, Is.EqualTo(DateTime.UtcNow.AddHours(1))
            .Within(TimeSpan.FromSeconds(10)));
    }

    [Test]
    public virtual void ValidUntil_is_not_corrupt_by_change_in_local()
    {
        var currentCulture = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            var cloudBlob = StubACloudBlob();
            var dateTime = new DateTime(2100, 1, 4, 12, 0, 0);
            var timeSpan = dateTime - DateTime.UtcNow;
            SetValidUntil(cloudBlob, timeSpan);
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-AU");
            var resultValidUntil = BlobStorageDataBus.GetValidUntil(cloudBlob);
            //Verify that day and month are not switched 
            Assert.AreEqual(4, resultValidUntil.Day);
            Assert.AreEqual(1, resultValidUntil.Month);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = currentCulture;
        }
    }

    [Test]
    [Explicit("Should not be possible to have a corrupted value")]
    public void ValidUntil_should_default_to_DateTimeMax_for_corrupted_value()
    {
        var cloudBlob = StubACloudBlob();
        SetValidUntil(cloudBlob, TimeSpan.FromHours(1));
        //HACK: set ValidUntil to be a non parsable string
        cloudBlob.Metadata["ValidUntil"] = "Not a date time";
        var resultValidUntil = BlobStorageDataBus.GetValidUntil(cloudBlob);
        Assert.AreEqual(DateTime.MaxValue, resultValidUntil);
    }

    [Test]
    public void ValidUntil_is_UtcKind()
    {
        var cloudBlob = StubACloudBlob();
        SetValidUntil(cloudBlob, TimeSpan.FromHours(1));
        var resultValidUntil = BlobStorageDataBus.GetValidUntil(cloudBlob);
        Assert.AreEqual(DateTimeKind.Utc, resultValidUntil.Kind);
    }

    [Test]
    public virtual void ValidUntil_defaults_to_DateTimeMax()
    {
        var cloudBlob = StubACloudBlob();

        SetValidUntil(cloudBlob, TimeSpan.MaxValue);
        var resultValidUntil = BlobStorageDataBus.GetValidUntil(cloudBlob);
        Assert.AreEqual(DateTime.MaxValue, resultValidUntil);
    }

    protected static ICloudBlob StubACloudBlob()
    {
        var cloudBlob = MockRepository.GenerateStub<ICloudBlob>();
        cloudBlob.Stub(x => x.Metadata)
            .Return(new Dictionary<string, string>());
        cloudBlob.Stub(x => x.SetMetadata());
        return cloudBlob;
    }

    protected abstract void SetValidUntil(ICloudBlob cloudBlob, TimeSpan timeSpan);
}

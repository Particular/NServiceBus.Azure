namespace NServiceBus.AcceptanceTests
{
    using NUnit.Framework;

    /// <summary>
    /// Base class for all the NSB test that sets up our conventions
    /// </summary>
    [TestFixture]
// ReSharper disable once PartialTypeWithSinglePart
    public abstract partial class NServiceBusAcceptanceTest
    {
        [TearDown]
        public void TearDown()
        {
           
        }
    }
}
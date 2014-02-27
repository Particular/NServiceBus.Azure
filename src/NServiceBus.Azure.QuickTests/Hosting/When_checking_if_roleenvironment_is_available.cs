namespace NServiceBus.Azure.QuickTests
{
    using Config;
    using NUnit.Framework;

    [TestFixture]
    [Category("Azure")]
    public class When_checking_if_roleenvironment_is_available
    {
        [Test]
        public void Should_not_throw_when_not_available()
        {
            Assert.AreEqual(false, SafeRoleEnvironment.IsAvailable);
        }
    }
}
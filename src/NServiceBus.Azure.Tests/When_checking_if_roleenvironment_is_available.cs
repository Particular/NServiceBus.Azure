namespace NServiceBus.Azure.Tests
{
    using Microsoft.WindowsAzure.ServiceRuntime;
    using NUnit.Framework;

    [TestFixture]
    [Category("Azure")]
    public class When_checking_if_roleenvironment_is_available
    {
        [Test]
        public void Should_not_throw_when_not_available()
        {
           Assert.AreEqual(false, RoleEnvironment.IsAvailable);
        }
    }
}
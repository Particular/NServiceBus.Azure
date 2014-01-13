    using NServiceBus.AcceptanceTests.Retries;
    using NUnit.Framework;

    /// <summary>
    /// Global setup fixture
    /// </summary>
    [SetUpFixture]
    public class SetupAcceptanceTests
    {
        [SetUp]
        public void SetUp()
        {
            When_doing_flr_with_default_settings.X = () => 4;
        }
    }

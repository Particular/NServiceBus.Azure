namespace NServiceBus.AzureStoragePersistence.Tests
{
    using System;
    using NUnit.Framework;
    using Saga;
    using SagaPersisters.Azure;

    [Explicit]
    public class When_saga_duplicate_exists : BaseAzureSagaPersisterTest
    {
        static readonly Guid ID1 = new Guid("E109827C-C9E2-48C0-91D9-D7D4C1ED3B51");
        static readonly Guid ID2 = new Guid("89ECE262-9D76-4D62-A0DA-243AA59663D8");

        [Test]
        public void Should_throw_exception()
        {
            Clear();

            var orderID = "order_id";
            var s1 = BuildState(orderID, ID1);
            var s2 = BuildState(orderID, ID2);

            Insert(s1);
            Insert(s2);

            Assert.Throws(Is.AssignableTo<DuplicatedSagaFoundException>(), () =>
            {
                var state = ((ISagaPersister) persister).Get<TwoInstanceSagaState>("OrderId", orderID);
                persister.Update(state);
            });
        }
    }
}
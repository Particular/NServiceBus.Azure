namespace NServiceBus.AzureStoragePersistence.Tests
{
    using System;
    using NUnit.Framework;
    using Saga;

    [Explicit]
    public class When_searched_by_property_without_existing_index : BaseAzureSagaPersisterTest
    {
        static readonly Guid ID1 = new Guid("46E0E0B1-B4FE-476A-8C25-0175424601CD");
        static readonly Guid ID2 = new Guid("0A63BB08-8951-4DF1-A149-121531C05F34");

        [Test]
        public void Should_create_index()
        {
            Clear();

            const string orderID = "order_id_1";

            var s1 = BuildState(orderID, ID1);
            var s2 = BuildState("order_id_2", ID2);

            Insert(s1);
            Insert(s2);

            var state = ((ISagaPersister) persister).Get<TwoInstanceSagaState>("OrderId", orderID);

            Assert.NotNull(state);
            Assert.AreEqual(ID1, state.Id);
            Assert.AreEqual(orderID, state.OrderId);
        }
    }
}
namespace NServiceBus.AzureStoragePersistence.Tests
{
    using NServiceBus.SagaPersisters.Azure.SecondaryIndeces;
    using NUnit.Framework;

    public class LRUCacheTests
    {
        public class When_cache_is_empty
        {
            private readonly LRUCache<int, int> Empty = new LRUCache<int, int>(0);

            [Test]
            public void Should_not_throw_on_remove()
            {
                Empty.Remove(1);
            }

            [Test]
            public void Should_not_find_any_value()
            {
                AssertNoValue(Empty, 1);
            }
        }

        public class When_cache_is_full
        {
            private LRUCache<int, int> cache;
            private const int Key1 = 1;
            private const int Key2 = 2;
            private const int Key3 = 3;
            private const int Value1 = 11;
            private const int Value11 = 111;
            private const int Value2 = 22;
            private const int Value3 = 32;

            [SetUp]
            public void SetUp()
            {
                cache = new LRUCache<int, int>(2);
                cache.Put(Key1, Value1);
                cache.Put(Key2, Value2);
            }

            [Test]
            public void Should_preserve_values()
            {
                AssertValue(cache, Key1, Value1);
                AssertValue(cache, Key2, Value2);
            }

            [Test]
            public void Should_add_new_value_removing_the_oldest()
            {
                cache.Put(Key3, Value3);

                AssertNoValue(cache, Key1);
                AssertValue(cache, Key2, Value2);
                AssertValue(cache, Key3, Value3);
            }

            [Test]
            public void Should_update_existing_value_reordering_lru_properly()
            {
                cache.Put(Key1, Value11);
                cache.Put(Key3, Value3);

                AssertNoValue(cache, Key2);
                AssertValue(cache, Key1, Value11);
                AssertValue(cache, Key3, Value3);
            }

            [Test]
            public void Should_create_a_slot_when_removing()
            {
                cache.Remove(Key2);
                cache.Put(Key3, Value3);

                AssertNoValue(cache, Key2);
                AssertValue(cache, Key1, Value1);
                AssertValue(cache, Key3, Value3);
            }
        }

        private static void AssertValue(LRUCache<int, int> lruCache, int key, int expectedValue)
        {
            int value;
            Assert.IsTrue(lruCache.TryGet(key, out value));
            Assert.AreEqual(expectedValue, value);
        }

        private static void AssertNoValue(LRUCache<int, int> lruCache, int key)
        {
            int value;
            var tryGet = lruCache.TryGet(key, out value);
            Assert.AreEqual(false, tryGet);
            Assert.AreEqual(default(int), value);
        }
    }
}
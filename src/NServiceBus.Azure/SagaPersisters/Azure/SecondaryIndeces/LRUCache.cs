namespace NServiceBus.SagaPersisters.Azure.SecondaryIndeces
{
    using System.Collections.Generic;

    public sealed class LRUCache<TKey, TValue>
    {
        readonly LinkedList<Item> lru = new LinkedList<Item>();
        readonly Dictionary<TKey, LinkedListNode<Item>> items = new Dictionary<TKey, LinkedListNode<Item>>();

        readonly int capacity;
        readonly object @lock = new object();

        private class Item
        {
            public TKey Key;
            public TValue Value;
        }

        public LRUCache(int capacity)
        {
            this.capacity = capacity;
        }

        public bool TryGet(TKey key, out TValue value)
        {
            lock (@lock)
            {
                LinkedListNode<Item> node;
                if (items.TryGetValue(key, out node))
                {
                    lru.Remove(node);
                    lru.AddLast(node);
                    value = node.Value.Value;
                    return true;
                }

                value = default(TValue);
                return false;
            }
        }

        public void Put(TKey key, TValue value)
        {
            lock (@lock)
            {
                LinkedListNode<Item> node;
                if (items.TryGetValue(key, out node) == false)
                {
                    node = new LinkedListNode<Item>(
                        new Item
                        {
                            Key = key,
                            Value = value
                        });
                    items.Add(key, node);

                    TrimOneIfNeeded();
                }
                else
                {
                    // just update the value
                    node.Value.Value = value;
                    lru.Remove(node);
                }

                lru.AddLast(node);
            }
        }

        public void Remove(TKey key)
        {
            lock (@lock)
            {
                LinkedListNode<Item> node;
                if (items.TryGetValue(key, out node))
                {
                    lru.Remove(node);
                    items.Remove(key);
                }
            }
        }

        private void TrimOneIfNeeded()
        {
            if (items.Count > 0 && items.Count > capacity)
            {
                var node = lru.First;
                lru.Remove(node);
                items.Remove(node.Value.Key);
            }
        }
    }
}
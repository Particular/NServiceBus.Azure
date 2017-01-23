namespace NServiceBus.SagaPersisters.Azure.SecondaryIndeces
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using Saga;

    /// <summary>
    /// A helper saga data serializer for internal purposes
    /// </summary>
    internal class SagaDataSerializer
    {
        public static byte[] SerializeSagaData<TSagaData>(TSagaData sagaData) where TSagaData : IContainSagaData
        {
            var serializer = new JsonSerializer
            {
                ContractResolver = new SagaOnlyPropertiesDataContractResolver()
            };

            using (var ms = new MemoryStream())
            {
                using (var zipped = new GZipStream(ms, CompressionMode.Compress))
                {
                    using (var sw = new StreamWriter(zipped))
                    {
                        serializer.Serialize(sw, sagaData);
                        sw.Flush();
                    }
                }

                return ms.ToArray();
            }
        }

        public static IContainSagaData DeserializeSagaData(Type sagaType, byte[] value)
        {
            var serializer = new JsonSerializer
            {
                ContractResolver = new SagaOnlyPropertiesDataContractResolver()
            };

            using (var ms = new MemoryStream(value))
            {
                using (var zipped = new GZipStream(ms, CompressionMode.Decompress))
                {
                    using (var sr = new StreamReader(zipped))
                    {
                        return (IContainSagaData)serializer.Deserialize(sr, sagaType);
                    }
                }
            }
        }

        private class SagaOnlyPropertiesDataContractResolver : DefaultContractResolver
        {
            public SagaOnlyPropertiesDataContractResolver() : base(true) // for performance
            {
            }

            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                var properties = new HashSet<string>(AzureSagaPersister.SelectPropertiesToPersist(type).Select(pi => pi.Name));
                return base.CreateProperties(type, memberSerialization).Where(p => properties.Contains(p.PropertyName)).ToArray();
            }
        }
    }
}
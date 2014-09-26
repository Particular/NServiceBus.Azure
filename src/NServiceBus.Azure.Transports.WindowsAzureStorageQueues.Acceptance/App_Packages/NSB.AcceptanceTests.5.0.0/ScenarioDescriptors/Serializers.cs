namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using System.Collections.Generic;
    using AcceptanceTesting.Support;

    public static class Serializers
    {
        public static RunDescriptor Binary = new RunDescriptor
            {
                Key = "Binary",
                Settings =
                    new Dictionary<string, string>
                        {
                            {
                                "Serializer", typeof (BinarySerializer).AssemblyQualifiedName
                            }
                        }
            };

        public static RunDescriptor Bson = new RunDescriptor
            {
                Key = "Bson",
                Settings =
                    new Dictionary<string, string>
                        {
                            {
                                "Serializer", typeof (BsonSerializer).AssemblyQualifiedName
                            }
                        }
            };

        public static RunDescriptor Xml = new RunDescriptor
            {
                Key = "Xml",
                Settings =
                    new Dictionary<string, string>
                        {
                            {
                                "Serializer", typeof (XmlSerializer).AssemblyQualifiedName
                            }
                        }
            };

        public static RunDescriptor Json = new RunDescriptor
            {
                Key = "Json",
                Settings =
                    new Dictionary<string, string>
                        {
                            {
                                "Serializer", typeof (JsonSerializer).AssemblyQualifiedName
                            }
                        }
            };
    }
}
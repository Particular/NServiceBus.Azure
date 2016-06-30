namespace NServiceBus
{
    namespace NServiceBus.Sagas
    {
        using System;

        /// <summary>
        /// The property that this saga is correlated on.
        /// </summary>
        internal class SagaCorrelationProperty
        {
            /// <summary>
            /// Initializes the correlation property.
            /// </summary>
            public SagaCorrelationProperty(string name, object value)
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                if (String.IsNullOrEmpty(name))
                {
                    throw new ArgumentException("Argument is null or empty", nameof(name));
                }

                Name = name;
                Value = value;
            }

            /// <summary>
            /// The name of the property.
            /// </summary>
            public string Name { get; private set; }

            /// <summary>
            /// The property value.
            /// </summary>
            public object Value { get; private set; }

            /// <summary>
            /// Represents a saga with no correlated property.
            /// </summary>
            public static SagaCorrelationProperty None { get; } = null;
        }
    }
}
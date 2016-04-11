namespace NServiceBus.SagaPersisters.Azure
{
    using System;

    public class DuplicatedSagaFoundException : Exception
    {
        public DuplicatedSagaFoundException(Type sagaType, string propertyName, params Guid[] identifiers)
            : base($"Sagas of type {sagaType.Name} with the following identifiers '{string.Join("', '", identifiers)}' are considered duplicates because of the violation of the Unique property {propertyName}.")
        {
            SagaType = sagaType;
            PropertyName = propertyName;
            Identifiers = identifiers;
        }

        public Type SagaType { get; }
        public Guid[] Identifiers { get; }
        public string PropertyName { get; }
    }
}
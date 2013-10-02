using NServiceBus;

namespace MyMessages
{
    public interface IDefineMessages : IMessage // lame, until we fix the messagemapper not to scan assemblies anymore
    {
    }
}
namespace WsPackets.Shared;

public interface ITypeResolver
{
    public Type? ResolveType(string name);
    public string? ResolveName(Type type);
}
using System.Reflection;

namespace WsPackets.Shared;

public class AssemblyTypeResolver : ITypeResolver
{
    private readonly string AssemblyPrefix;
    private readonly Assembly Assembly;
    
    public AssemblyTypeResolver(Assembly assembly, string assemblyPrefix)
    {
        Assembly = assembly;
        AssemblyPrefix = assemblyPrefix;
    }

    public Type? ResolveType(string name)
    {
        return Assembly.GetType($"{AssemblyPrefix}.{name}");
    }

    public string? ResolveName(Type type)
    {
        if (type.FullName == null)
            return null;
        
        return type.FullName.Replace($"{AssemblyPrefix}.", "");
    }
}
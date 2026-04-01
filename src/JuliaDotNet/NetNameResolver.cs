using System.Reflection;
using System.Runtime.Loader;

namespace JuliaDotNet;

public class NetNameResolver
{
    private readonly AssemblyLoadContext _context = AssemblyLoadContext.Default;
    private readonly HashSet<string> _loadedNames = new();
    private readonly Dictionary<string, Type> _loadedTypes = new();

    public NetNameResolver()
    {
        
    }

    public Assembly UsingAssembly(Assembly assembly) {
        return assembly;
    }
    
    public Assembly UsingAssemblyByName(string name) => UsingAssembly(_context.LoadFromAssemblyName(new AssemblyName(name)));
    public Assembly UsingAssemblyByPath(string path) => UsingAssembly(_context.LoadFromAssemblyPath(path));
    
    public object Using(string path) {
        if (_loadedTypes.TryGetValue(path, out var ty))
            return ty;
        
        if (!_loadedNames.Contains(path)) {
            ty = FindType(path);
            if (ty != null) {
                _loadedTypes.Add(path, ty);
                return ty;
            }

            if (IsNamespace(path)) {
                _loadedNames.Add(path);
                return path;
            }
        }
        throw new EntryPointNotFoundException("Cannot find path: " + path);
    }

    public bool IsNamespace(string name) {
        return _context.Assemblies
            .SelectMany(a => a.GetTypes())
            .Any(t => t.Namespace != null && 
                      (t.Namespace == name || t.Namespace.StartsWith(name + ".")));
    }

    private Type? LoadType(string name) {
        foreach (var asm in _context.Assemblies) {
            var ty = asm.GetType(name);
            if (ty != null)
                return ty;
        }
        return null;
    }
    
    public Type? FindType(string typeName) {
        var ty = LoadType(typeName);
        if (ty != null)
            return ty;
        foreach (var ns in _loadedNames) {
            if ((ty = LoadType(ns + "." + typeName)) != null)
                return ty;
        }
        return null;
    }
    
    
}
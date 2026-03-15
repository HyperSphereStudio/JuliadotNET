using System.Buffers;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CSharp.RuntimeBinder;
using Binder = Microsoft.CSharp.RuntimeBinder.Binder;

namespace JuliaDotNet;

public enum CallOp
{
    GetMember,
    SetMember,
    Invoke,
    Unary,
    Binary,
    GetIndex,
    SetIndex
}

public class NetReflectionFactory
{
    public delegate object? VariadicFcn(object[] args, int nargs);
    public delegate CallSiteBinder BinderFactory(string name, bool isStatic, bool isVoid, int nargs, CSharpArgumentInfo[] args);

    private static readonly ParameterExpression[] ps = [
        Expression.Parameter(typeof(object[]), "args"),
        Expression.Parameter(typeof(int), "len")
    ];

    private static bool TryBindToStaticGetField(Type type, string name, out VariadicFcn? fcn) {
        var fieldInfo = type.GetField(name,
            BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);

        if (fieldInfo == null)
        {
            fcn = null;
            return false;
        }

        fcn = Expression
            .Lambda<VariadicFcn>(Expression.Convert(Expression.Field(null, fieldInfo), typeof(object)), ps)
            .Compile();
        return true;
    }

    private static bool TryBindToStaticSetField(Type type, string name, out VariadicFcn? fcn)
    {
        var fieldInfo = type.GetField(name,
            BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
        if (fieldInfo == null)
        {
            fcn = null;
            return false;
        }

        var expr = Expression.Assign(Expression.Field(null, fieldInfo),
            Expression.Convert(Expression.ArrayIndex(ps[0], Expression.Constant(1)), fieldInfo.FieldType));

        var cexpr = Expression.Convert(expr, typeof(object));

        fcn = Expression.Lambda<VariadicFcn>(cexpr, ps).Compile();
        return true;
    }

    private static bool TryBindToStaticSetProperty(Type type, string name, out VariadicFcn? fcn)
    {
        var pInfo = type.GetProperty(name,
            BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);

        if (pInfo == null)
        {
            fcn = null;
            return false;
        }

        var expr = Expression.Assign(Expression.Property(null, pInfo),
            Expression.Convert(Expression.ArrayIndex(ps[0], Expression.Constant(1)), pInfo.PropertyType));
        var cexpr = Expression.Convert(expr, typeof(object));

        fcn = Expression.Lambda<VariadicFcn>(cexpr, ps).Compile();
        return true;
    }

    private static bool TryBindToStaticGetProperty(Type type, string name, out VariadicFcn? fcn)
    {
        var pInfo = type.GetProperty(name,
            BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);

        if (pInfo == null)
        {
            fcn = null;
            return false;
        }

        fcn = Expression
            .Lambda<VariadicFcn>(Expression.Convert(Expression.Property(null, pInfo), typeof(object)), ps)
            .Compile();

        return true;
    }

    public static VariadicFcn MakeGet(bool isStatic, bool isVoid, Type targetType, string name) {
        if (isStatic)
        {
            if (TryBindToStaticGetField(targetType, name, out var fcn) ||
                TryBindToStaticGetProperty(targetType, name, out fcn))
                return fcn;
        }

        return CreateSite(name, isStatic, isVoid, 1, (name2, _, isvoid, _, ai) =>
            Binder.GetMember(isvoid ? CSharpBinderFlags.ResultDiscarded : 0, name2, typeof(object), ai));
    }
    
    public static VariadicFcn MakeSet(bool isStatic, bool isVoid, Type targetType, string name) {
        if (isStatic) {
            if (TryBindToStaticSetField(targetType, name, out var fcn) ||
                TryBindToStaticSetProperty(targetType, name, out fcn))
                return fcn;
        }

        return CreateSite(name, isStatic, isVoid, 2, (name2, _, isvoid, _, ai) =>
            Binder.SetMember(isvoid ? CSharpBinderFlags.ResultDiscarded : 0, name2, typeof(object), ai));
    }

    public static VariadicFcn MakeInvoke(bool isStatic, int nargs, bool isVoid, string name, Type? targetType) {
        return CreateSite(name, isStatic, isVoid, nargs, (name2, _, isvoid, _, ai) => {
            var flags = isvoid ? CSharpBinderFlags.ResultDiscarded : 0;
            if (name == "Invoke" && isStatic) {
                return Binder.InvokeConstructor(flags, targetType, ai);
            }
            return Binder.InvokeMember(flags, name2, null, typeof(object), ai);
        });
    }

    public static VariadicFcn MakeBinaryOperation(ExpressionType bop) {
        return CreateSite(bop.ToString(), false, false, 2, (_, _, _, _, ai) =>
            Binder.BinaryOperation(CSharpBinderFlags.None, bop, typeof(object), ai));
    }
    
    public static VariadicFcn MakeUnaryOperation(ExpressionType bop) {
        return CreateSite(bop.ToString(), false, false, 1, (_, _, _, _, ai) =>
            Binder.UnaryOperation(CSharpBinderFlags.None, bop, typeof(object), ai));
    }
    
    public static VariadicFcn MakeGetIndex(int nargs, bool isVoid) {
        return CreateSite("getindex", false, isVoid, nargs, (_, _, isvoid, _, ai) =>
            Binder.GetIndex(isvoid ? CSharpBinderFlags.ResultDiscarded : 0, typeof(object), ai));
    }
    
    public static VariadicFcn MakeSetIndex(int nargs, bool isVoid) {
        return CreateSite("setindex", false, isVoid, nargs, (_, _, isvoid, _, ai) =>
            Binder.SetIndex(isvoid ? CSharpBinderFlags.ResultDiscarded : 0, typeof(object), ai));
    }
    
    private static VariadicFcn CreateSite(string name, bool isStatic, bool isVoid, int nargs, BinderFactory factory) {
        var argInfos = new CSharpArgumentInfo[nargs + 1];
        var flags = isStatic
            ? CSharpArgumentInfoFlags.IsStaticType | CSharpArgumentInfoFlags.UseCompileTimeType
            : CSharpArgumentInfoFlags.None;
        
        argInfos[0] = CSharpArgumentInfo.Create(flags, null);

        for (var i = 1; i < argInfos.Length; i++)
            argInfos[i] = CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null);

        // Site + Target + Args + ReturnType
        var typeArgs = new Type[nargs + 1 + (isVoid ? 0 : 1)];
        typeArgs[0] = typeof(CallSite);
        for (var i = 0; i < nargs; i++) typeArgs[i + 1] = typeof(object);
        
        if(!isVoid)
            typeArgs[^1] = typeof(object); // Return

        var delegateType = isVoid ? Expression.GetActionType(typeArgs) : Expression.GetFuncType(typeArgs);
        var binder = factory(name, isStatic, isVoid, nargs, argInfos);
        
        var cs = typeof(CallSite<>)
            .MakeGenericType(delegateType)
            .GetMethod("Create")!
            .Invoke(null, [binder])!;

        var csc = Expression.Constant(cs);
        var targetFi = Expression.Field(csc, cs.GetType().GetField("Target")!);

        var argPs = new Expression[nargs + 1];
        argPs[0] = csc;
        for (var i = 0; i < nargs; i++)
            argPs[i + 1] = Expression.ArrayIndex(ps[0], Expression.Constant(i));

        var invokeExpr = Expression.Invoke(targetFi, argPs);
        
        Expression retExpr = invokeExpr;
        
        if(!isVoid)
            retExpr = Expression.Convert(retExpr, typeof(object));
        else {
            retExpr = Expression.Block(retExpr, Expression.Constant(null));
        }

        return Expression.Lambda<VariadicFcn>(retExpr, ps).Compile();
    }
}

public class NetReflection
{
    private readonly int _capacity;

    private readonly record struct CacheEntry(NetReflectionFactory.VariadicFcn Site, long LastAccessed);
    public readonly record struct CacheKey(string Name, CallOp Op, int Nargs, Type TargetType, bool Static, bool Void);

    private readonly ConcurrentDictionary<CacheKey, CacheEntry> _cache = new();

    public NetReflection(int capacity = 500) => _capacity = capacity;

    public NetReflectionFactory.VariadicFcn GetOrAdd(CacheKey key, Func<CacheKey, NetReflectionFactory.VariadicFcn> factory) {
        long now = DateTime.UtcNow.Ticks;

        if (_cache.TryGetValue(key, out var entry)) {
            _cache[key] = entry with { LastAccessed = now };
            return entry.Site;
        }

        var newSite = factory(key);
        var newEntry = new CacheEntry(newSite, now);

        if (_cache.TryAdd(key, newEntry)) {
            if (_cache.Count > _capacity) {
                EvictOldest();
            }
        }

        return newSite;
    }

    public object? Get(object target, string name, bool isVoid) {
        var targetType = target as Type ?? target.GetType();
        var site = GetOrAdd(new(name, CallOp.GetMember, 1, targetType, target is Type, isVoid), 
            k => NetReflectionFactory.MakeGet(k.Static, k.Void, k.TargetType, k.Name));

        var args = ArrayPool<object>.Shared.Rent(1);
        try {
            args[0] = target;
            return site(args, 1);
        }
        finally
        {
            ArrayPool<object>.Shared.Return(args);
        }
    }

    public void Set(object target, string name, object? value, bool isVoid) {
        var targetType = target is Type t ? t : target.GetType();
        var site = GetOrAdd(new(name, CallOp.SetMember, 2, targetType, target is Type, isVoid), k => 
            NetReflectionFactory.MakeSet(k.Static, k.Void, k.TargetType, k.Name));

        var args = ArrayPool<object>.Shared.Rent(2);
        try {
            args[0] = target;
            args[1] = value;
            site(args, 2);
        }
        finally
        {
            ArrayPool<object>.Shared.Return(args);
        }
    }

    public object? Invoke(object target, string name, Span<object> args, bool isVoid) {
        var argCount = args.Length + 1;
        var targetType = target as Type ?? target.GetType();
        var site = GetOrAdd(new(name, CallOp.Invoke, argCount, targetType, target is Type, isVoid), 
            k => NetReflectionFactory.MakeInvoke(k.Static, k.Nargs, k.Void, k.Name, k.TargetType));
        
        var fargs = ArrayPool<object>.Shared.Rent(1 + args.Length);
        try {
            fargs[0] = target;
            for (var i = 0; i < args.Length; i++)
                fargs[1 + i] = args[i];
            return site(fargs, 1 + args.Length);
        }
        finally {
            ArrayPool<object>.Shared.Return(fargs);
        }
    }

    public object? BinaryOperation(ExpressionType bop, object a, object b)
    {
        var site = GetOrAdd(new(bop.ToString(), CallOp.Binary, 2, null, false, false), 
            k => NetReflectionFactory.MakeBinaryOperation(bop));
        var fargs = ArrayPool<object>.Shared.Rent(2);
        try {
            fargs[0] = a;
            fargs[1] = b;
            return site(fargs, 2);
        }
        finally
        {
            ArrayPool<object>.Shared.Return(fargs);
        }
    }

    public object? UnaryOperation(object target, ExpressionType bop)
    {
        var site = GetOrAdd(new(bop.ToString(), CallOp.Unary, 1, target.GetType(), false, false), 
            k => NetReflectionFactory.MakeUnaryOperation(bop));
        var fargs = ArrayPool<object>.Shared.Rent(1);
        try
        {
            fargs[0] = target;
            return site(fargs, 1);
        }
        finally
        {
            ArrayPool<object>.Shared.Return(fargs);
        }
    }

    public object? GetIndex(object target, Span<object> idxs, bool isVoid)
    {
        var argCount = idxs.Length + 1;
        var targetType = target is Type t ? t : target.GetType();
        var site = GetOrAdd(new("getindex", CallOp.GetIndex, argCount, targetType, target is Type, isVoid), 
            k => NetReflectionFactory.MakeGetIndex(k.Nargs, isVoid));
        var fargs = ArrayPool<object>.Shared.Rent(1 + idxs.Length);
        try {
            fargs[0] = target;
            idxs.CopyTo(fargs.AsSpan(1));
            return site(fargs, 1 + idxs.Length);
        }
        finally {
            ArrayPool<object>.Shared.Return(fargs);
        }
    }

    public object? SetIndex(object target, object value, Span<object> idxs, bool isVoid)
    {
        var argCount = idxs.Length + 1;
        var targetType = target as Type ?? target.GetType();
        var site = GetOrAdd(new("setindex", CallOp.SetIndex, argCount + 1, targetType, target is Type, isVoid), 
            k => NetReflectionFactory.MakeSetIndex(k.Nargs, k.Void));

        var fargs = ArrayPool<object>.Shared.Rent(2 + idxs.Length);
        try {
            fargs[0] = target;
            for (var i = 0; i < idxs.Length; i++)
                fargs[1 + i] = idxs[i];
            fargs[1 + idxs.Length] = value;
            return site(fargs, 2 + idxs.Length);
        }
        finally
        {
            ArrayPool<object>.Shared.Return(fargs);
        }
    }


    private void EvictOldest()
    {
        var oldest = _cache.OrderBy(kvp => kvp.Value.LastAccessed).Take(10);
        foreach (var j in oldest)
        {
            _cache.TryRemove(j.Key, out _);
        }
    }
}
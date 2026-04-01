using System.Buffers;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace JuliaDotNet;

using static JuliaCalls;

public unsafe class Interop
{
    // Existing properties...
    public static IntPtr StringF { get; private set; }
    public static IntPtr GetPropertyF { get; private set; }
    public static IntPtr SetPropertyF { get; private set; }
    public static IntPtr GetExceptionF { get; internal set; }
    public static IntPtr ConvertF { get; private set; }
    public static IntPtr CompileDelegateToJuliaF { get; internal set; }

    // Missing Core Functions
    public static IntPtr GetIndexF { get; private set; } // For obj[i]
    public static IntPtr SetIndexF { get; private set; } // For obj[i] = v
    public static IntPtr IterateF { get; private set; } // For foreach loops
    public static IntPtr LengthF { get; private set; } // For .Length
    public static IntPtr ShowF { get; private set; } // For debugging

    // Singletons
    public static JAny NothingA { get; private set; }
    public static JAny TrueA { get; private set; }
    public static JAny FalseA { get; private set; }

    // Arithmetic & Logic
    public static IntPtr AddF { get; private set; }
    public static IntPtr SubF { get; private set; }
    public static IntPtr MultiplyF { get; private set; }
    public static IntPtr DivideF { get; private set; }
    public static IntPtr ModuloF { get; private set; }
    public static IntPtr PowerF { get; private set; }

    // Missing Logic & Bitwise
    public static IntPtr NotF { get; private set; } // !obj
    public static IntPtr BitAndF { get; private set; } // &
    public static IntPtr BitOrF { get; private set; } // |
    public static IntPtr BitXorF { get; private set; } // ^ (bitwise)
    public static IntPtr BitShiftLF { get; private set; } // <<
    public static IntPtr BitShiftRF { get; private set; } // >>
    public static IntPtr BitNotF { get; private set; } // >>

    // Comparison
    public static IntPtr EqF { get; private set; } // ==
    public static IntPtr GreaterThenEqF { get; private set; }
    public static IntPtr GreaterThenF { get; private set; }
    public static IntPtr LessThanF { get; private set; }
    public static IntPtr LessThanEqF { get; private set; }

    // Numeric Types
    public static IntPtr AnyT { get; private set; }
    public static IntPtr BoolT { get; private set; }
    public static IntPtr Int8T { get; private set; }
    public static IntPtr UInt8T { get; private set; }
    public static IntPtr Int16T { get; private set; }
    public static IntPtr UInt16T { get; private set; }
    public static IntPtr Int32T { get; private set; }
    public static IntPtr UInt32T { get; private set; }
    public static IntPtr Int64T { get; private set; }
    public static IntPtr UInt64T { get; private set; }
    public static IntPtr Float16T { get; private set; }
    public static IntPtr Float32T { get; private set; }
    public static IntPtr Float64T { get; private set; }
    public static IntPtr SharpObjectT { get; internal set; }
    public static IntPtr StringT { get; private set; }
    public static IntPtr SymbolT { get; private set; }
    public static IntPtr CharT { get; private set; }
    public static IntPtr TypeT { get; private set; }
    public static IntPtr NothingT { get; private set; }
    public static IntPtr VoidPtrT { get; private set; }
    public static IntPtr ComplexF64T { get; private set; }

    internal static delegate* unmanaged[Cdecl]<IntPtr, IntPtr> TypeOfF { get; private set; }
    internal static delegate* unmanaged[Cdecl]<IntPtr, long> CreateJuliaRoot { get; private set; }
    internal static delegate* unmanaged[Cdecl]<long, void> FreeJuliaRoot { get; private set; }
    internal static delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr> GetArrayPointer { get; private set; }
    internal static delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr> IterateForSharp { get; private set; }
    internal static delegate* unmanaged[Cdecl]<long, IntPtr> CreateSharpObject { get; private set; }
    internal static delegate* unmanaged[Cdecl]<IntPtr, long> UnboxSharpObject { get; private set; }
    
    internal static void Init(bool jlInterfaceIsLoaded) {
        // Numeric Primitives
        BoolT = jl_eval_string("Bool");
        Int8T = jl_eval_string("Int8");
        UInt8T = jl_eval_string("UInt8");
        Int16T = jl_eval_string("Int16");
        UInt16T = jl_eval_string("UInt16");
        Int32T = jl_eval_string("Int32");
        UInt32T = jl_eval_string("UInt32");
        Int64T = jl_eval_string("Int64");
        UInt64T = jl_eval_string("UInt64");
        Float16T = jl_eval_string("Float16");
        Float32T = jl_eval_string("Float32");
        Float64T = jl_eval_string("Float64");
        StringT = jl_eval_string("String");
        SymbolT = jl_eval_string("Symbol");
        CharT = jl_eval_string("Char");
        TypeT = jl_eval_string("Type");
        VoidPtrT = jl_eval_string("Ptr{Nothing}");
        NothingT = jl_eval_string("Nothing");
        ComplexF64T = jl_eval_string("ComplexF64");
        AnyT = jl_eval_string("Any");

        // Core System
        StringF = jl_eval_string("string");
        GetPropertyF = jl_eval_string("getproperty");
        SetPropertyF = jl_eval_string("setproperty!");
        GetIndexF = jl_eval_string("getindex");
        SetIndexF = jl_eval_string("setindex!");
        IterateF = jl_eval_string("iterate");
        LengthF = jl_eval_string("length");
        ConvertF = jl_eval_string("convert");

        // Singletons
        NothingA = new(jl_eval_string("nothing"), false);
        TrueA = new(jl_eval_string("true"), false);
        FalseA = new(jl_eval_string("false"), false);

        // Arithmetic
        AddF = jl_eval_string("+");
        SubF = jl_eval_string("-");
        MultiplyF = jl_eval_string("*");
        DivideF = jl_eval_string("/");
        ModuloF = jl_eval_string("%");
        PowerF = jl_eval_string("^");

        // Logic & Bitwise
        NotF = jl_eval_string("!");
        BitAndF = jl_eval_string("&");
        BitNotF = jl_eval_string("~");
        BitOrF = jl_eval_string("|");
        BitXorF = jl_eval_string("xor");
        BitShiftLF = jl_eval_string("<<");
        BitShiftRF = jl_eval_string(">>");

        // Comparison
        EqF = jl_eval_string("==");
        GreaterThenEqF = jl_eval_string(">=");
        GreaterThenF = jl_eval_string(">");
        LessThanF = jl_eval_string("<");
        LessThanEqF = jl_eval_string("<=");

        if (!jlInterfaceIsLoaded) {
            var bytes = GetResourceBytes("JuliaDotNet.JuliaDotNet.jl");
            jl_eval_string(Encoding.UTF8.GetString(bytes));
            Julia.CheckExceptions();
            jl_eval_string("using .JuliaDotNet");   
        }
        else {
            jl_eval_string("using JuliaDotNet"); 
        }
        Julia.CheckExceptions();
        
        GetExceptionF = jl_eval_string("JuliaDotNet.get_backtrace_str");
      
        FreeJuliaRoot = (delegate* unmanaged[Cdecl]<long, void>)
            JLConvert.Unbox<IntPtr>(jl_eval_string("@cfunction(JuliaDotNet.unroot_object_from_sharp, Cvoid, (Int, ))"));
        
        TypeOfF = (delegate* unmanaged[Cdecl]<IntPtr, IntPtr>)
            JLConvert.Unbox<IntPtr>(jl_eval_string("@cfunction(Base.typeof, Any, (Any, ))"));
        
        SharpObjectT = jl_eval_string("JuliaDotNet.SharpObject");
       
        CreateJuliaRoot = (delegate* unmanaged[Cdecl]<IntPtr, long>)
            JLConvert.Unbox<IntPtr>(jl_eval_string("@cfunction(JuliaDotNet.root_object_from_sharp, Int, (Any, ))"));
   
        CompileDelegateToJuliaF = jl_eval_string("JuliaDotNet.compile_delegate");
        
        GetArrayPointer = (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr>) 
            JLConvert.UnboxPtr(jl_eval_string("""
                                              @cfunction(JuliaDotNet.get_array_ptr_void, Ptr{Cvoid}, (Any, Ptr{Int}))
                                              """));
        
        IterateForSharp = (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr>) 
            JLConvert.UnboxPtr(jl_eval_string("""
                                              @cfunction(JuliaDotNet.iterate_for_sharp, Any, (Any, Ptr{Any}))
                                              """));
        
        CreateSharpObject = (delegate* unmanaged[Cdecl]<long, IntPtr>) 
            JLConvert.UnboxPtr(jl_eval_string("""
                                              @cfunction(JuliaDotNet.create_sharp_object, Any, (Int, ))
                                              """));
        
        UnboxSharpObject = (delegate* unmanaged[Cdecl]<IntPtr, long>) 
            JLConvert.UnboxPtr(jl_eval_string("""
                                              @cfunction(JuliaDotNet.handle, Int, (Any, ))
                                              """));
    
        Julia.CheckExceptions();
    }
    
    public static byte[] GetResourceBytes(string resourceName)
    {
        using Stream stream = typeof(Interop).Assembly.GetManifestResourceStream(resourceName)!;
        if (stream == null) {
            Console.WriteLine("Resources Found:" + string.Join(",", typeof(Interop).Assembly.GetManifestResourceNames()));
            throw new FileNotFoundException("Resource not found", resourceName);
        }
        using MemoryStream ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}

public static class SharpInterop{
    private static readonly object RootsLock = new();
    private static readonly Dictionary<long, object> ObjectRoots = new();
    private static Int64 _nextId = 1;
    private static NetReflection _reflect = new(1000);
    
    public static long CreateSharpObjectHandle(object o) {
        lock (RootsLock) {
            var id = _nextId++;
            ObjectRoots[id] = o;
            return id;
        }
    }

    public static object? GetObjectFromHandle(long id) {
        if (id == 0)
            return null;
        lock (RootsLock) {
            return ObjectRoots[id];
        }
    }
    
    public static IntPtr ConvertObjectToJulia(object? o, JulianArgFlags flags) {
        if (flags.HasFlag(JulianArgFlags.TryConvertToJuliaNative)) {
            return JLConvert.BoxToJulia(o);
        }
        return JLConvert.BoxAsSharpObject(o);
    }
    
    public static unsafe Delegate CreateDelegateFromJuliaObject(Type dtype, JAny x, long _pflags, long nPars, long returnFlags) {
        JulianArgFlags[]? pFlags = null;
        var paramFlags = (long*) _pflags;
        if (paramFlags != null) {
            pFlags = new JulianArgFlags[nPars];
            for(var i = 0; i < nPars; i++)
                pFlags[i] = (JulianArgFlags) paramFlags[i];
        }
        return Julia.CreateDelegateFromJuliaObject(dtype, x, pFlags, (JulianArgFlags) returnFlags);
    }
    
    public static object ConvertObjectToSharp(IntPtr x, JulianArgFlags flags) {
        if (flags.HasFlag(JulianArgFlags.TryConvertToSharpNative)) {
            if (JLConvert.TryConvertJuliaToSharp(x, out var argv)) {
                return argv;
            } 
        }
        return new JAny(x);
    }
    

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe IntPtr NetReflect(long targetID, IntPtr opSym, JulianArg* args, long nargs, JulianArgFlags returnFlags) {
        var isVoidRet = returnFlags.HasFlag(JulianArgFlags.DiscardReturn);
        
        if (!Symbols.TryGetStringFromSymbol(opSym, out var op)) {
            jl_error("Op must be Symbol!");
            return 0;
        }
     
        var tar = GetObjectFromHandle(targetID);
    
        if (tar == null) {
            jl_error("Null Target");
            return 0;
        }
        
        if (op == "getproperty" && nargs == 1 && Symbols.TryGetStringFromSymbol(args[0].V, out var pname)) {
            return JLConvert.BoxToJulia(_reflect.Get(tar, pname!, isVoidRet));
        }
        
        var argObjects = ArrayPool<object>.Shared.Rent((int) nargs);
        try {
            for (var i = 0; i < nargs; i++) {
                var arg = args[i];
                argObjects[i] =  ConvertObjectToSharp(arg.V, arg.Flags);
            }
            
            var ro = op switch {
                "setproperty" when nargs == 2 && Symbols.TryGetStringFromSymbol(args[0].V, out pname) => _reflect.Set(tar, pname!, argObjects[1], true),
                "getindex" => _reflect.GetIndex(tar, argObjects.AsSpan(0, (int)nargs), isVoidRet),
                "setindex!" => _reflect.SetIndex(tar, argObjects[0], argObjects.AsSpan(1, (int)nargs - 1), isVoidRet),
                "invokeMember" when Symbols.TryGetStringFromSymbol(args[0].V, out var invokeMember) => _reflect.Invoke(
                    tar, invokeMember!, argObjects.AsSpan(1, (int)nargs - 1), isVoidRet),
                "binary_op" when nargs == 3 &&
                                 Symbols.TryGetStringFromSymbol(args[0].V, out var bop) &&
                                 Enum.TryParse<ExpressionType>(bop!, out var bexp) =>
                    _reflect.BinaryOperation(bexp, argObjects[1], argObjects[2]),
                "unary_op" when nargs == 2 && Symbols.TryGetStringFromSymbol(args[0].V, out var bop) &&
                                Enum.TryParse<ExpressionType>(bop!, out var uexp) => _reflect.UnaryOperation(argObjects[1], uexp),
                "invoke" => _reflect.Invoke(tar, "Invoke", argObjects.AsSpan(0, (int)nargs), isVoidRet),
                _ => throw new Exception("Unknown operation:" + op)
            };
            return ConvertObjectToJulia(ro, returnFlags);
            
        }catch (Exception e) {
            var sb = new StringBuilder("Exception Occured During .NET Reflection:");
            sb.Append(op);
            sb.Append('(');
            for (var i = 0; i < nargs; i++) {
                if (i != 0) sb.Append(", ");
                sb.Append(argObjects[i]);
                sb.Append("::");
                sb.Append(argObjects[i]?.GetType().Name ?? "null");
                if (argObjects[i] is JAny ja) {
                    sb.Append('[');
                    sb.Append(ja.TypeOfName);
                    sb.Append(']');
                }
            }
            sb.AppendLine(")");
            
            var ex = new Exception(sb.ToString(), e);
            jl_throw(JAny.BoxSharpObject(ex).Handle);
        }
        finally {
            ArrayPool<object?>.Shared.Return(argObjects);
        }
        
        return 0;
    }
    
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void unroot_object_from_julia(long id) {
        lock (RootsLock) {
            ObjectRoots.Remove(id);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static IntPtr ToString(long id) => JLConvert.BoxRaw(GetObjectFromHandle(id)?.ToString() ?? "null");
    
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static long ToHash(long id) => GetObjectFromHandle(id)?.GetHashCode() ?? 0;
    
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static IntPtr GetType(long id) => JLConvert.BoxAsSharpObject(GetObjectFromHandle(id)!.GetType());
    
    internal static unsafe void Init() { 
        dynamic jdn = Julia.Eval("JuliaDotNet");
        dynamic vars = new JAny(JuliaArrays.AllocArray(JuliaArrays.CreateArrayType(Interop.AnyT, 1), 6));
        Julia.Eval("");
        
        delegate* unmanaged[Cdecl]<long, void> unroot = &unroot_object_from_julia;
        
        vars[1] = JAny.Box((IntPtr) unroot);
        
        delegate* unmanaged[Cdecl]<long, IntPtr, JulianArg*, long, JulianArgFlags, IntPtr> netreflect = &NetReflect;
        vars[2] = JAny.Box((IntPtr) netreflect);
        
        vars[3] = JAny.BoxSharpObject(typeof(NetNameResolver));
        
        delegate* unmanaged[Cdecl]<long, IntPtr> nettostring = &ToString;
        vars[4] = JAny.Box((IntPtr) nettostring);
        
        delegate* unmanaged[Cdecl]<long, long> nethash = &ToHash;
        vars[5] = JAny.Box((IntPtr) nethash);
        
        delegate* unmanaged[Cdecl]<long, IntPtr> nettype = &GetType;
        vars[6] = JAny.Box((IntPtr) nettype);
   
        jdn.init_sharp_fcns(vars);
        
        Julia.CheckExceptions();
    }
    
}

public enum JulianArgFlags : long{
    Nothing = 0,
    TryConvertToSharpNative = 1,
    TryConvertToJuliaNative = 2,
    DiscardReturn = 4
}

public record struct JulianArg(IntPtr V, JulianArgFlags Flags);


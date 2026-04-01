using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace JuliaDotNet;

public class JuliaOptions {
    public string JuliaDirectory = "";
    public List<string> Arguments = new();
    public int ThreadCount = 1;
    public int WorkerCount = 1;
    public int Optimize = 2;
    public string LoadSystemImage;
    public string EvaluationString;
    public bool UseSystemImageNativeCode = true;
    public bool HandleSignals = true;
    public bool PrecompileModules = true;

    public void Add(params object[] args) {
        foreach (var arg in args)
            Arguments.Add(arg.ToString());
    }

    private string AsJLString(bool b) => b ? "yes" : "no";

    internal void BuildArguments() {
        Add("");

        if (ThreadCount != 1)
            Add("-t", ThreadCount);

        if (WorkerCount != 1)
            Add("-p", WorkerCount);

        if (Optimize != 2)
            Add("-O", Optimize);

        if (EvaluationString != null)
            Add("-e", EvaluationString);

        if (LoadSystemImage != null)
            Add("-J", LoadSystemImage);

        if (!UseSystemImageNativeCode)
            Add("--sysimage-native-code=", AsJLString(UseSystemImageNativeCode));

        if (!PrecompileModules)
            Add("--compiled-modules=", AsJLString(PrecompileModules));

        if(!HandleSignals)
            Add("--handle-signals =", AsJLString(PrecompileModules));

        if (string.IsNullOrEmpty(JuliaDirectory)) {
            var path = Path.Join(JuliaLocator.GetJuliaPath((path, v) => v is { Major: 1, Minor: 11 } ? v.Patch : 0), "bin");
            JuliaDirectory = path ?? throw new FileNotFoundException("JuliaDirectory not found");
        }
    }
}

public class Julia
{
    public static bool Initialized { get; private set; }
    
    public static void Init(JuliaOptions opts) {
        if(Initialized)
            throw new Exception("Julia already initialized");
        opts.BuildArguments();
        var env = Environment.CurrentDirectory;
        Environment.CurrentDirectory = opts.JuliaDirectory;
        jl_init_code(opts);
        Environment.CurrentDirectory = env;
        Initialized = true;
    }
    
    [UnmanagedCallersOnly]
    public static int InitFromNative() {
        AfterJuliaInit(true);
        return 43;
    }

    public static void Exit(int code) {
        JuliaCalls.jl_exit(code);
        Initialized = false;
    }

    public static unsafe IntPtr Typeof(IntPtr jObject) => Interop.TypeOfF(jObject);
    public static JAny Eval(string s) => new(JuliaCalls.jl_eval_string(s));

    public static unsafe JAny CompileDelegateToJFunction(Delegate d, IntPtr returnType, params IntPtr[] jParameters) {
        var args = stackalloc IntPtr[jParameters.Length + 2];
        args[0] = JLConvert.BoxRaw((IntPtr)Marshal.GetFunctionPointerForDelegate(d));
        args[1] = returnType;
        for(var i = 0; i < jParameters.Length; i++)
            args[i + 2] = jParameters[i];
        return new(JuliaCalls.jl_call(Interop.CompileDelegateToJuliaF, args, jParameters.Length + 1));
    }
    
    public static unsafe IntPtr Invoke(IntPtr f, params Span<IntPtr> parameters) {
        fixed (IntPtr* pms = parameters) {
            return JuliaCalls.jl_call(f, pms, parameters.Length);
        }
    }


    private static readonly MethodInfo InvokeUnsafeDelegateMI = typeof(Julia).GetMethod("InvokeUnsafeDelegate", BindingFlags.NonPublic | BindingFlags.Static)!;
    internal static unsafe object InvokeUnsafeDelegate(JuliaDelegateContext ctx, object[] jparameters) {
        var pms = stackalloc IntPtr[jparameters.Length];
        for (var i = 0; i < jparameters.Length; i++)
            pms[i] = SharpInterop.ConvertObjectToJulia(jparameters[i], ctx.ParameterFlags[i]);
        var v = JuliaCalls.jl_call(ctx.F.Handle, pms, jparameters.Length);
        CheckExceptions();
        return SharpInterop.ConvertObjectToSharp(v, ctx.ReturnFlags);
    }
    
    private static void jl_init_code(JuliaOptions opts) {
        var arguments = opts.Arguments.ToArray();
        if (arguments.Length != 0) {
            int len = arguments.Length;
            unsafe {
                var stringBytes = stackalloc byte*[arguments.Length];
                Span<GCHandle> handles = stackalloc GCHandle[arguments.Length];

                for (int i = 0; i < arguments.Length; ++i) {
                    handles[i] = GCHandle.Alloc(Encoding.ASCII.GetBytes(arguments[i]), GCHandleType.Pinned);
                    stringBytes[i] = (byte*) handles[i].AddrOfPinnedObject();
                }
        
                JuliaCalls.jl_parse_opts(ref len, &stringBytes);

                foreach(var handle in handles)
                    handle.Free();
            }
        }
        
        JuliaCalls.jl_init();
        AfterJuliaInit(false);
    }

    private static void AfterJuliaInit(bool jlInterfaceIsLoaded) {
        Interop.Init(jlInterfaceIsLoaded);
        SharpInterop.Init();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)] public static void CheckExceptions() {
        var ex = JuliaCalls.jl_exception_occurred();
        if (ex != 0) {
            throw JuliaException.GetException(ex);
        }
    }
    
    

    public static Delegate CreateDelegateFromJuliaObject(Type dtype, JAny x, JulianArgFlags[]? paramFlags, JulianArgFlags returnFlags) {
        var invoke = dtype.GetMethod("Invoke")!;
        var pars = invoke.GetParameters();
        var ptypes = new Type[pars.Length + 1];
        ptypes[0] = typeof(JuliaDelegateContext);
        for (var i = 0; i < pars.Length; ++i)
            ptypes[i + 1] = pars[i].ParameterType;
        
        var dm = new DynamicMethod("JuliaDelegate", 
            MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, 
            invoke.ReturnType, ptypes, typeof(Julia), true);
        
        var il = dm.GetILGenerator();
        
        var cnt = ptypes.Length - 1;
        //args = new object[cnt];
        var arr = il.DeclareLocal(typeof(object[]));
        il.Emit(OpCodes.Ldc_I4, cnt);
        il.Emit(OpCodes.Newarr, typeof(object));
        il.Emit(OpCodes.Stloc, arr);
        
        for (var i = 0; i < cnt; ++i) {
            il.Emit(OpCodes.Ldloc, arr);    // Load the array reference
            il.Emit(OpCodes.Ldc_I4, i);     // Load the index
            il.Emit(OpCodes.Ldarg, i + 1);  // Load the method argument (skip context)
    
            var ptype = ptypes[i + 1];
            if (ptype.IsValueType) {
                il.Emit(OpCodes.Box, ptype);
            }
    
            il.Emit(OpCodes.Stelem_Ref); 
        }
        
        //InvokeUnsafeDelegate(ctx, args[]);
        il.Emit(OpCodes.Ldarg_0);       //ctx
        il.Emit(OpCodes.Ldloc, arr);    //args[]
        il.EmitCall(OpCodes.Call, InvokeUnsafeDelegateMI, null);
        if(invoke.ReturnType.IsValueType)
            il.Emit(OpCodes.Unbox_Any, invoke.ReturnType);
        il.Emit(OpCodes.Ret);

        if (paramFlags == null) {
            paramFlags = new JulianArgFlags[cnt];
            paramFlags.AsSpan().Fill(JulianArgFlags.TryConvertToSharpNative);
        }
        
        return dm.CreateDelegate(dtype, new JuliaDelegateContext{ParameterFlags = paramFlags, ReturnFlags = returnFlags, F = x});
    }

    internal class JuliaDelegateContext {
        public JAny F;
        public JulianArgFlags[] ParameterFlags;
        public JulianArgFlags ReturnFlags;
    }
}


public class JuliaException : Exception {
    private static string GetMessage(IntPtr ptr) {
        try {
            if (Interop.GetExceptionF != 0)
                return JLConvert.UnboxString(JuliaCalls.jl_call1(Interop.GetExceptionF, ptr));
            return JLConvert.UnboxString(JuliaCalls.jl_call1(Interop.StringF, ptr));
        }catch (Exception e) {
            Console.WriteLine("Error Writing Exception To Console!");
            Console.WriteLine(e);
            Console.WriteLine(ptr.ToString());
            throw;
        }
    }

    public static Exception GetException(IntPtr ptr) {
        if (JuliaCalls.jl_isa(ptr, Interop.SharpObjectT) != 0) {
            var o = JLConvert.UnboxSharpObject(ptr);
            if (o is Exception e)
                return e;
        }
        return new JuliaException(ptr);
    }
        
    public JuliaException(IntPtr excep) : base(GetMessage(excep)) => JuliaCalls.jl_exception_clear();
}
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using JULIAdotNET;

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

    public static Delegate CompileJFunctionToDelegate(Type delegateType, IntPtr fun) {
        return null;
    }
    
    public static unsafe IntPtr Invoke(IntPtr f, params Span<IntPtr> parameters) {
        fixed (IntPtr* pms = parameters) {
            return JuliaCalls.jl_call(f, pms, parameters.Length);
        }
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
        Interop.Init(false);
        SharpInterop.Init();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)] public static void CheckExceptions() {
        var ex = JuliaCalls.jl_exception_occurred();
        if (ex != 0) {
            throw new JuliaException(ex);
        }
    }
}
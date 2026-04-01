using System.Collections;
using System.Dynamic;
using System.Linq.Expressions;

namespace JuliaDotNet;

using static JuliaCalls;
using static JLConvert;

public struct Symbols {
    private static readonly Dictionary<string, IntPtr> _symhandles = new();
    private static readonly Dictionary<IntPtr, string> _sym2str = new();
    private static readonly Dictionary<string, JAny> _syms = new();

    public static JAny GetSymbol(string sym) {
        if (_syms.TryGetValue(sym, out var value)) 
            return value;
        var handle = GetSymbolHandle(sym);
        value = new JAny(handle, false);
        _syms.Add(sym, value);
        return value;
    }

    public static IntPtr GetSymbolHandle(string sym) {
        if (_symhandles.TryGetValue(sym, out var value)) 
            return value;
        value = jl_symbol(sym);
        _symhandles.Add(sym, value);
        _sym2str.Add(value, sym);
        return value;
    }

    public static bool TryGetStringFromSymbol(IntPtr sym, out string? sym_str) {
        if (_sym2str.TryGetValue(sym, out var value)) {
            sym_str = value;
            return true;
        }

        if (jl_isa(sym, Interop.SymbolT) == 0) {
            sym_str = null;
            return false;
        }
        
        var str = UnboxString(jl_call1(Interop.StringF, sym));
        _sym2str[sym] = str;
        _symhandles[str] = sym;
        sym_str = str;
        return true;
    }
}

public class JAny : DynamicObject, IDisposable, IEnumerable<JAny> {
    public IntPtr Handle { get; private set; }
    public long RootHandle { get; private set; }

    public unsafe JAny(IntPtr handle, bool root = true) {
        Handle = handle;
        if (root) {
            Julia.CheckExceptions();
            RootHandle = Interop.CreateJuliaRoot(handle);
        }
    }

    ~JAny() {
        try {
            Dispose();
        }
        catch (Exception ex) {
            Console.WriteLine("Caught Error While Disposing:" + ex);
        }
    }

    public unsafe void Dispose() {
        GC.SuppressFinalize(this);
        if (RootHandle != 0) {
            Interop.FreeJuliaRoot(RootHandle);
            Julia.CheckExceptions();
            RootHandle = 0;
        }
        Handle = 0;
    }

    public IntPtr TypeOf => Julia.Typeof(Handle);
    public string TypeOfName => jl_typeof_str(Handle)!;

    public static JAny BoxSharpObject(object o) => new(BoxAsSharpObject(o));
    public static JAny Box(IntPtr ptr) => new(BoxRaw(ptr));
    public static JAny Box(long l) =>  new(BoxRaw(l));
    public static JAny Box(ulong l) => new(BoxRaw(l));
    public static JAny Box(bool b) => b ? Interop.TrueA : Interop.FalseA;
    public static JAny Box(int l) => new(BoxRaw(l));
    public static JAny Box(uint l) => new(BoxRaw(l));
    public static JAny Box(short l) => new(BoxRaw(l));
    public static JAny Box(ushort l) => new(BoxRaw(l));
    public static JAny Box(byte l) => new(BoxRaw(l));
    public static JAny Box(sbyte l) => new(BoxRaw(l));
    public static JAny Box(float l) => new(BoxRaw(l));
    public static JAny Box(double l) => new(BoxRaw(l));
    public static JAny Box(string s) => new(BoxRaw(s));
    public static JAny BoxObject(object o) => new(JLConvert.BoxToJulia(o));

    public T Unbox<T>() where T : unmanaged => LoadDataFromHandle<T>(Handle);
    public ulong UnboxUInt64() => JLConvert.UnboxUInt64(Handle);
    public long UnboxInt64() => JLConvert.UnboxInt64(Handle);
    public uint UnboxUInt32() => JLConvert.UnboxUInt32(Handle);
    public int UnboxInt32() => JLConvert.UnboxInt32(Handle);
    public ushort UnboxUInt16() => JLConvert.UnboxUInt16(Handle);
    public short UnboxInt16() => JLConvert.UnboxInt16(Handle);
    public byte UnboxUInt8() => JLConvert.UnboxUInt8(Handle);
    public sbyte UnboxInt8() => JLConvert.UnboxInt8(Handle);
    public bool UnboxBool() => JLConvert.UnboxBool(Handle);
    public double UnboxFloat64() => JLConvert.UnboxFloat64(Handle);
    public float UnboxFloat32() => JLConvert.UnboxFloat32(Handle);
    public IntPtr UnboxPtr() => JLConvert.UnboxPtr(Handle);
    public string UnboxString() => JLConvert.UnboxString(Handle);
    public object UnboxSharpObject() => JLConvert.UnboxSharpObject(Handle);
    
    public JAny ConvertTo(IntPtr jType) => new(JLConvert.ConvertTo(Handle, jType));

    public override bool TryGetMember(GetMemberBinder binder, out object? result) {
        result = new JAny(jl_call2(Interop.GetPropertyF, Handle, Symbols.GetSymbolHandle(binder.Name)));
        Julia.CheckExceptions();
        return true;
    }

    public override bool TrySetMember(SetMemberBinder binder, object? value) {
        jl_call3(Interop.SetPropertyF, Handle, Symbols.GetSymbolHandle(binder.Name), JLConvert.BoxToJulia(value));
        Julia.CheckExceptions();
        return true;
    }

    public override unsafe bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result) {
        var cnt = (args?.Length ?? 0);
        var pms = stackalloc IntPtr[cnt];
        var f = jl_call2(Interop.GetPropertyF, Handle, Symbols.GetSymbolHandle(binder.Name));
        for (var i = 0; i < cnt; i++)
            pms[i] = JLConvert.BoxToJulia(args[i]);
        
        result = new JAny(jl_call(f, pms, cnt));
        
        return true;
    }

    public override unsafe bool TryInvoke(InvokeBinder binder, object?[]? args, out object? result) {
        var cnt = (args?.Length ?? 0);
        var pms = stackalloc IntPtr[cnt];
        for (var i = 0; i < cnt; i++)
            pms[i] = JLConvert.BoxToJulia(args[i]);
        result = new JAny(jl_call(Handle, pms, cnt));
        return true;
    }

    // --- Indexing (Arrays) ---
    public override unsafe bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object? result) {
        var cnt = (indexes?.Length ?? 0) + 1;
        var pms = stackalloc IntPtr[cnt];
        pms[0] = Handle;
        for (var i = 1; i < cnt; i++)
            pms[i] = JLConvert.BoxToJulia(indexes[i - 1]);
        result = jl_call(Interop.GetIndexF, pms, cnt);
        Julia.CheckExceptions();
        return true;
    }

    public override unsafe bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value) {
        var cnt = (indexes?.Length ?? 0) + 2;
        var pms = stackalloc IntPtr[cnt];
        pms[0] = Handle;
        pms[1] = JLConvert.BoxToJulia(value);
        for (var i = 2; i < cnt; i++)
            pms[i] = JLConvert.BoxToJulia(indexes[i - 2]);
        jl_call(Interop.SetIndexF, pms, cnt);
        Julia.CheckExceptions();
        return true;
    }

    // --- Arithmetic & Comparison ---
    public override bool TryBinaryOperation(BinaryOperationBinder binder, object arg, out object? result) {
        var jop = GetBinaryOperation(binder.Operation);
        result = new JAny(jl_call2(jop, Handle, JLConvert.BoxToJulia(arg)));
        return true;
    }

    public override bool TryUnaryOperation(UnaryOperationBinder binder, out object? result) {
        var jop = GetUnaryOperation(binder.Operation);
        result = new JAny(jl_call1(jop, Handle));
        return true;
    }

    public override bool TryConvert(ConvertBinder binder, out object? result) {
        return JLConvert.TryConvert(Handle, binder.Type, out result);
    }

    public override string ToString() => JLConvert.UnboxString(jl_call1(Interop.StringF, Handle));
    public IEnumerator GetEnumerator() => new JAnyEnumerator(this);
    IEnumerator<JAny> IEnumerable<JAny>.GetEnumerator() => new JAnyEnumerator(this);
    
    public struct JAnyEnumerator : IEnumerator<JAny> {
        private readonly JAny _collection;
        private IntPtr _state;      
        private JAny _current; 

        public JAnyEnumerator(JAny collection) {
            _collection = collection;
            _state = Interop.NothingA.Handle;
        }

        public unsafe bool MoveNext() {
            var nState = _state;
            _current = new JAny(Interop.IterateForSharp(_collection.Handle, (IntPtr) (&nState)));
            _state = nState;
            return _state != Interop.NothingA.Handle;
        }

        public void Reset() {
            _state = Interop.NothingA.Handle;
        }

        public JAny Current => _current;
        object IEnumerator.Current => Current;
        public void Dispose() { }
    }
}
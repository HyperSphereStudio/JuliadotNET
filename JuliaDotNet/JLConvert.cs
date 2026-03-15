using System.Linq.Expressions;
using System.Numerics;
using System.Runtime.InteropServices;

namespace JuliaDotNet;

using static JuliaCalls;

public static class JLConvert
{
    public static IntPtr BoxRaw(IntPtr ptr) => jl_box_voidpointer(ptr);
    public static IntPtr BoxRaw(long l) => jl_box_int64(l);
    public static IntPtr BoxRaw(ulong l) => jl_box_uint64(l);
    public static IntPtr BoxRaw(bool b) => b ? Interop.TrueA.Handle : Interop.FalseA.Handle;
    public static IntPtr BoxRaw(int l) => jl_box_int32(l);
    public static IntPtr BoxRaw(uint l) => jl_box_uint32(l);
    public static IntPtr BoxRaw(short l) => jl_box_int16(l);
    public static IntPtr BoxRaw(ushort l) => jl_box_uint16(l);
    public static IntPtr BoxRaw(byte l) => jl_box_uint8(l);
    public static IntPtr BoxRaw(sbyte l) => jl_box_int8(l);
    public static IntPtr BoxRaw(float l) => jl_box_float32(l);
    public static IntPtr BoxRaw(double l) => jl_box_float64(l);
    public static IntPtr BoxRaw(string s) => jl_cstr_to_string(s);

    public static unsafe IntPtr BoxAsSharpObject(object? o) => o == null
        ? Interop.NothingA.Handle
        : Interop.CreateSharpObject(SharpInterop.CreateSharpObjectHandle(o));
    
    public static IntPtr BoxToJulia(object? o) {
        return o switch {
            null => Interop.NothingA.Handle,
            JAny ja => ja.Handle,
            bool b => jl_box_bool(b),
            sbyte i8 => jl_box_int8(i8),
            byte u8 => jl_box_uint8(u8),
            short i16 => jl_box_int16(i16),
            ushort u16 => jl_box_uint16(u16),
            int i32 => jl_box_int32(i32),
            uint u32 => jl_box_uint32(u32),
            long i64 => jl_box_int64(i64),
            ulong u64 => jl_box_uint64(u64),
            float f32 => jl_box_float32(f32),
            double f64 => jl_box_float64(f64),
            string s => jl_cstr_to_string(s),
            _ => BoxAsSharpObject(o)
        };
    }
    
    public static IntPtr ConvertTo(IntPtr o, IntPtr jType) => new(jl_call2(Interop.ConvertF, jType, o));
    public static T Unbox<T>(IntPtr l) where T : unmanaged => LoadDataFromHandle<T>(l);
    public static ulong UnboxUInt64(IntPtr l) => LoadDataFromHandle<ulong>(l);
    public static long UnboxInt64(IntPtr l) => LoadDataFromHandle<long>(l);
    public static uint UnboxUInt32(IntPtr l) => LoadDataFromHandle<uint>(l);
    public static int UnboxInt32(IntPtr l) => LoadDataFromHandle<int>(l);
    public static ushort UnboxUInt16(IntPtr l) => LoadDataFromHandle<ushort>(l);
    public static short UnboxInt16(IntPtr l) => LoadDataFromHandle<short>(l);
    public static byte UnboxUInt8(IntPtr l) => LoadDataFromHandle<byte>(l);
    public static sbyte UnboxInt8(IntPtr l) => LoadDataFromHandle<sbyte>(l);
    public static bool UnboxBool(IntPtr l) => LoadDataFromHandle<bool>(l);
    public static double UnboxFloat64(IntPtr l) => LoadDataFromHandle<double>(l);
    public static float UnboxFloat32(IntPtr l) => LoadDataFromHandle<float>(l);
    public static IntPtr UnboxPtr(IntPtr l) => LoadDataFromHandle<IntPtr>(l);
    public static string UnboxString(IntPtr l) => jl_string_ptr(l) ?? "null";
    public static unsafe object? UnboxSharpObject(IntPtr l) => SharpInterop.GetObjectFromHandle(Interop.UnboxSharpObject(l));

    public static IntPtr? GetJuliaEqType(Type? t) {
        if (t == null)
            return Interop.NothingT;
        if (t == typeof(bool))
            return Interop.BoolT;
        if (t == typeof(byte))
            return Interop.UInt8T;
        if (t == typeof(sbyte))
            return Interop.Int8T;
        if (t == typeof(short))
            return Interop.Int16T;
        if (t == typeof(ushort))
            return Interop.UInt16T;
        if (t == typeof(int))
            return Interop.Int32T;
        if (t == typeof(uint))
            return Interop.UInt32T;
        if (t == typeof(long))
            return Interop.Int64T;
        if (t == typeof(ulong))
            return Interop.UInt64T;
        if (t == typeof(float))
            return Interop.Float32T;
        if (t == typeof(double))
            return Interop.Float64T;
        if (t == typeof(string))
            return Interop.StringT;
        if (t == typeof(Complex))
            return Interop.ComplexF64T;
        if (t == typeof(char))
            return Interop.CharT;
        if (t == typeof(object))
            return Interop.SharpObjectT;
        return null;
    }
    
    public static Type? GetSharpEqType(IntPtr ty) {
        if (jl_types_equal(ty, Interop.NothingT) != 0)
            return typeof(object);
        if (jl_types_equal(ty, Interop.BoolT) != 0)
            return typeof(bool);
        if (jl_types_equal(ty, Interop.UInt8T) != 0)
            return typeof(byte);
        if (jl_types_equal(ty, Interop.Int8T) != 0)
            return typeof(sbyte);
        if (jl_types_equal(ty, Interop.Int16T) != 0)
            return typeof(short);
        if (jl_types_equal(ty, Interop.UInt16T) != 0)
            return typeof(ushort);
        if (jl_types_equal(ty, Interop.Int32T) != 0)
            return typeof(int);
        if (jl_types_equal(ty, Interop.UInt32T) != 0)
            return typeof(uint);
        if (jl_types_equal(ty, Interop.Int64T) != 0)
            return typeof(long);
        if (jl_types_equal(ty, Interop.UInt64T) != 0)
            return typeof(ulong);
        if (jl_types_equal(ty, Interop.Float32T) != 0)
            return typeof(float);
        if (jl_types_equal(ty, Interop.Float64T) != 0)
            return typeof(double);
        if (jl_types_equal(ty, Interop.StringT) != 0)
            return typeof(string);
        if (jl_types_equal(ty, Interop.CharT) != 0)
            return typeof(char);
        if (jl_types_equal(ty, Interop.ComplexF64T) != 0)
            return typeof(Complex);
        if (jl_types_equal(ty, Interop.VoidPtrT) != 0)
            return typeof(IntPtr);
        if (jl_types_equal(ty, Interop.SharpObjectT) != 0)
            return typeof(object);
        return null;
    }

    public static bool TryConvertJuliaToSharp(IntPtr handle, out object? result) {
        var ty = GetSharpEqType(Julia.Typeof(handle));
        result = null;
        return ty != null && TryConvert(handle, ty, out result);
    }

   
    public static bool TryConvert(IntPtr handle, Type t, out object? result) {
        if (handle == Interop.NothingA.Handle) {
            result = null;
            return true;
        }

        var jet = GetJuliaEqType(t);

        if (jet != null) {
            var x = ConvertTo(handle, jet!.Value);
            
            if (t == typeof(char)) {
                result = (char)UnboxUInt32(x);
                return true;
            }

            if (t.IsValueType) {
                if (t == typeof(bool)) {
                    result = UnboxBool(x);
                    return true;
                }
                if (t == typeof(int)) {
                    result = UnboxInt32(x);
                    return true;
                }
                if (t == typeof(float)) {
                    result = UnboxFloat32(x);
                    return true;
                }
                if (t == typeof(double)) {
                    result = UnboxFloat64(x);
                    return true;
                }
                if (t == typeof(long)) {
                    result = UnboxInt64(x);
                    return true;
                }
                result = Marshal.PtrToStructure(x, t);
                return true;
            }
            
            if (t == typeof(string)) {
                result = UnboxString(ConvertTo(handle, Interop.StringT));
                return true;
            }

            if (t == typeof(object) && jl_isa(handle, Interop.SharpObjectT) != 0) {
                result = UnboxSharpObject(Unbox<IntPtr>(handle));
                return true;
            }
        }
        
        result = null;
        return false;
    }
    
    public static IntPtr GetBinaryOperation(ExpressionType bop) => bop switch {
        ExpressionType.Add => Interop.AddF,
        ExpressionType.Subtract => Interop.SubF,
        ExpressionType.Multiply => Interop.MultiplyF,
        ExpressionType.Divide => Interop.DivideF,
        ExpressionType.Modulo => Interop.ModuloF,
        ExpressionType.Power => Interop.PowerF,
        ExpressionType.GreaterThan => Interop.GreaterThenF,
        ExpressionType.GreaterThanOrEqual => Interop.GreaterThenEqF,
        ExpressionType.LessThan => Interop.LessThanF,
        ExpressionType.LessThanOrEqual => Interop.LessThanEqF,
        ExpressionType.And => Interop.BitAndF,
        ExpressionType.Or => Interop.BitOrF,
        ExpressionType.LeftShift => Interop.BitShiftLF,
        ExpressionType.RightShift => Interop.BitShiftRF,
        _ => throw new InvalidOperationException(bop.ToString())
    };
    
    public static IntPtr GetUnaryOperation(ExpressionType uop) => uop switch {
        ExpressionType.Negate => Interop.SubF,
        ExpressionType.Not => Interop.NotF,
        ExpressionType.OnesComplement => Interop.BitNotF, 
        _ => throw new InvalidOperationException(uop.ToString())
    };

    public static unsafe T* GetStructureDataFromJPtr<T>(IntPtr handle) where T : unmanaged {
        return (T*)handle;
    }
    
    public static unsafe T LoadDataFromHandle<T>(IntPtr handle, int el = 0) where T : unmanaged {
        return GetStructureDataFromJPtr<T>(handle)[el];
    }
    
}
//Written By Johnathan Bizzano

namespace JuliaDotNet;

using System;
using System.Runtime.InteropServices;

public class JuliaCalls
{
    public enum JLIMAGESEARCH
    {
        JL_IMAGE_CWD = 0,
        JL_IMAGE_JULIA_HOME = 1,
        //JL_IMAGE_LIBJULIA = 2,
    }

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern void julia_init(JLIMAGESEARCH rel);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern void jl_init();

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe void jl_parse_opts(ref int argc, byte*** argvp);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr jl_eval_string([MarshalAs(UnmanagedType.LPStr)] string str);
    
    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr jl_ptr_to_array_1d(IntPtr atype, IntPtr data, long nel, int own_buffer);
    
    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern void jl_atexit_hook(int hook);
    
    [DllImport("libjulia", EntryPoint = "jl_string_ptr", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr _jl_string_ptr(IntPtr v);
    public static string? jl_string_ptr(IntPtr v) => Marshal.PtrToStringUTF8(_jl_string_ptr(v));

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern double jl_unbox_float64(IntPtr t);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern float jl_unbox_float32(IntPtr t);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern long jl_unbox_int64(IntPtr t);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern int jl_unbox_int32(IntPtr t);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern short jl_unbox_int16(IntPtr t);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern sbyte jl_unbox_int8(IntPtr t);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool jl_unbox_bool(IntPtr t);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong jl_unbox_uint64(IntPtr t);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint jl_unbox_uint32(IntPtr t);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern ushort jl_unbox_uint16(IntPtr t);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern byte jl_unbox_uint8(IntPtr t);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr jl_unbox_voidpointer(IntPtr v);


    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr jl_box_float64(double t);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr jl_box_float32(float t);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr jl_box_int64(long t);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr jl_box_int32(int t);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr jl_box_int16(short t);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr jl_box_int8(sbyte t);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr jl_box_bool(bool t);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr jl_box_uint64(ulong t);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr jl_box_uint32(uint t);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr jl_box_uint16(ushort t);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr jl_box_uint8(byte t);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr jl_box_voidpointer(IntPtr x);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr jl_cstr_to_string(string s);
    
    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static unsafe extern IntPtr jl_call(IntPtr f, IntPtr* args, Int32 arg_count);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr jl_call0(IntPtr f);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr jl_call1(IntPtr f, IntPtr arg1);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr jl_call2(IntPtr f, IntPtr arg1, IntPtr arg2);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr jl_call3(IntPtr f, IntPtr arg1, IntPtr arg2, IntPtr arg3);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr jl_get_global(IntPtr m, IntPtr var);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr jl_symbol(string sym);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern int jl_types_equal(IntPtr v1, IntPtr v2);


    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr jl_exception_occurred();

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr jl_current_exception();

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern void jl_exception_clear();


    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr jl_typename_str(IntPtr val);

    [DllImport("libjulia", EntryPoint = "jl_typeof_str", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr _jl_typeof_str(IntPtr v);
    public static string? jl_typeof_str(IntPtr v) => Marshal.PtrToStringUTF8(_jl_typeof_str(v));
    
    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe IntPtr jl_alloc_array_nd(IntPtr aType, long* dims, long ndims);
    
    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr jl_apply_array_type(IntPtr elType, long dim);
    
    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr jl_module_globalref(IntPtr m, IntPtr var);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern void jl_set_global(IntPtr m, IntPtr var, IntPtr val);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern void jl_set_const(IntPtr m, IntPtr var, IntPtr val);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern void jl_declare_constant(IntPtr b);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern int jl_cpu_threads();

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern long jl_getpagesize();

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern long jl_getallocationgranularity();

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern int jl_is_debugbuild();

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr jl_get_UNAME();

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr jl_get_ARCH();

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr jl_get_libllvm();

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr jl_environ(int i);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern void jl_error(string str);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr jl_get_libdir();

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern void jl_init_with_image(string julia_bindir, string image_relative_path);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr jl_get_default_sysimg_path();

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern int jl_is_initialized();


    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern void jl_exit(int status);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern void jl_yield();

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern void jl_no_exc_handler(IntPtr e);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern int jl_gc_enable(int on);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern int jl_gc_is_enabled();

    public enum JLGCCollection
    {
        JL_GC_AUTO = 0, // use heuristics to determine the collection type
        JL_GC_FULL = 1, // force a full collection
        JL_GC_INCREMENTAL = 2, // force an incremental collection
    };

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern void jl_gc_collect(JLGCCollection c);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static unsafe extern IntPtr jl_malloc_stack(uint* bufsz, IntPtr owner);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern void jl_free_stack(IntPtr stkbuf, nint bufsz);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern void jl_gc_use(IntPtr a);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr jl_gc_managed_malloc(nint sz);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern int jl_subtype(IntPtr a, IntPtr b);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern int jl_isa(IntPtr a, IntPtr t);

    [DllImport("libjulia", CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe IntPtr* jl_get_pgcstack();
}
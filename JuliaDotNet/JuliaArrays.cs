namespace JuliaDotNet;

public static class JuliaArrays {
    
    public static IntPtr CreateVectorFromArrayType<T>(IntPtr aType, params T[] v) where T:unmanaged {
        var a = AllocArray(aType, v.Length);
        var ptr = GetArraySpan<T>(a);
        v.CopyTo(ptr);
        return a;
    }

    public static IntPtr CreateArrayType(IntPtr elType, long dim) => JuliaCalls.jl_apply_array_type(elType, dim);

    public static unsafe IntPtr AllocArray(IntPtr aType, params long[] dims) {
        fixed (long* pDims = dims) {
            return JuliaCalls.jl_alloc_array_nd(aType, pDims, dims.Length);
        }
    }
    public static IntPtr CreateVector<T>(IntPtr aType, params T[] v) where T:unmanaged {
        var a = AllocArray(aType, v.Length);
        var ptr = GetArraySpan<T>(a);
        v.CopyTo(ptr);
        return a;
    }

    public static unsafe T* GetArrayPtr<T>(IntPtr jArray, out long length) where T : unmanaged {
        long len = 0;
        var ptr = Interop.GetArrayPointer(jArray, (IntPtr) (&len));
        length = len;
        return (T*) ptr;
    }
    
    public static unsafe Span<T> GetArraySpan<T>(IntPtr jArray) where T : unmanaged {
        var ptr = GetArrayPtr<T>(jArray, out var length);
        return new(ptr, (int) length);
    }
    
}

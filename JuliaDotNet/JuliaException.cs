using JuliaDotNet;

namespace JULIAdotNET;

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
        
    public JuliaException(IntPtr excep) : base(GetMessage(excep)) => JuliaCalls.jl_exception_clear();
}